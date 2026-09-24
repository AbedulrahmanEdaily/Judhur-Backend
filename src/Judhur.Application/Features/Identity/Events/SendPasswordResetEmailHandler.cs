using Judhur.Application.Common.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Events;

public sealed class SendPasswordResetEmailHandler(IIdentityService identityService, IEmailQueue emailQueue, ILogger<SendPasswordResetEmailHandler> logger)
    : INotificationHandler<PasswordResetRequested>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailQueue _emailQueue = emailQueue;
    private readonly ILogger<SendPasswordResetEmailHandler> _logger = logger;

    public async Task Handle(PasswordResetRequested notification, CancellationToken cancellationToken)
    {
        var result = await _identityService.BuildPasswordResetEmailAsync(notification.UserId, cancellationToken);
        if (result.IsError)
        {
            _logger.LogError("Password reset email could not be built for user {UserId}: {ErrorCode}", notification.UserId, result.TopError.Code);
            return;
        }
        await _emailQueue.EnqueueAsync(result.Value, cancellationToken);
        _logger.LogInformation("Password reset email queued for user {UserId}", notification.UserId);
    }
}