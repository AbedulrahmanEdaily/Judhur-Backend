using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Judhur.Infrastructure.Email;

internal sealed class EmailBackgroundService(ChannelEmailQueue emailQueue, FrontendSettings frontendSettings, IServiceScopeFactory scopeFactory, ILogger<EmailBackgroundService> logger) : BackgroundService
{
    private readonly ChannelEmailQueue _emailQueue = emailQueue;
    private readonly FrontendSettings _frontendSettings = frontendSettings;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<EmailBackgroundService> _logger = logger;
    private const int MaxAttempts = 3;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _emailQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(request, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Gave up on the confirmation email for {UserId} after {MaxAttempts} attempts.",
                    request.UserId,
                    MaxAttempts);
            }
        }
    }
    private async Task ProcessAsync(EmailConfirmationRequest request, CancellationToken stoppingToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                var user = await userManager.FindByIdAsync(request.UserId.ToString());
                if (user?.Email is null)
                {
                    _logger.LogWarning(
                        "No user or no address for {UserId}; dropping the confirmation email.",
                        request.UserId);
                    return;
                }
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var link = $"{_frontendSettings.ConfirmEmailUrl}" +
                            $"?userId={user.Id}" +
                            $"&token={Uri.EscapeDataString(token)}";
                var message = new EmailMessage(
                    user.Email,
                    "Confirm your email address",
                    $"""<p>Welcome to Judhur.</p><p><a href="{link}">Confirm your email address</a></p>""");
                await emailSender.SendEmailAsync(message, stoppingToken);
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts && !stoppingToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(
                    exception,
                    "Attempt {Attempt} of {MaxAttempts} failed for {UserId}. Retrying in {Delay}.",
                    attempt,
                    MaxAttempts,
                    request.UserId,
                    delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}