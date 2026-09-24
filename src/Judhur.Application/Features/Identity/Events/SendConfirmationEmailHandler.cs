using Judhur.Application.Common.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Events;

public sealed class SendConfirmationEmailHandler(IIdentityService identityService, IEmailQueue emailQueue, ILogger<SendConfirmationEmailHandler> logger)
    : INotificationHandler<UserRegistered>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailQueue _emailQueue = emailQueue;
    private readonly ILogger<SendConfirmationEmailHandler> _logger = logger;

    public async Task Handle(UserRegistered notification, CancellationToken cancellationToken)
    {
        var result = await _identityService.BuildConfirmationEmailAsync(notification.UserId, cancellationToken);
        if (result.IsError)
        {
            _logger.LogError("Confirmation email could not be built for user {UserId}: {ErrorCode}", notification.UserId, result.TopError.Code);
            return;
        }
        await _emailQueue.EnqueueAsync(result.Value, cancellationToken);
        _logger.LogInformation("Confirmation email queued for user {UserId}", notification.UserId);
    }
}