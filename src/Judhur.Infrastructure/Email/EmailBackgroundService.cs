using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Judhur.Infrastructure.Email;

internal sealed class EmailBackgroundService(ChannelEmailQueue emailQueue, IServiceScopeFactory scopeFactory, ILogger<EmailBackgroundService> logger) : BackgroundService
{
    private readonly ChannelEmailQueue _emailQueue = emailQueue;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<EmailBackgroundService> _logger = logger;
    private const int MaxAttempts = 3;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _emailQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Gave up on the email to {To} after {MaxAttempts} attempts.",
                    message.To,
                    MaxAttempts);
            }
        }
    }
    private async Task ProcessAsync(EmailMessage message, CancellationToken stoppingToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await emailSender.SendEmailAsync(message, stoppingToken);
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts && !stoppingToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(
                    exception,
                    "Attempt {Attempt} of {MaxAttempts} failed for the email to {To}.Retrying in {Delay}.",
                    attempt,
                    MaxAttempts,
                    message.To,
                    delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}