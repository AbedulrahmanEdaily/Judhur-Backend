using Judhur.Application.Common.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Events;

public sealed class SendPasswordChangedHandler(IIdentityService identityService, IEmailQueue emailQueue, ILogger<SendPasswordChangedHandler> logger) : INotificationHandler<PasswordChanged>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailQueue _emailQueue = emailQueue;
    private readonly ILogger<SendPasswordChangedHandler> _logger = logger;

    public async Task Handle(PasswordChanged notification, CancellationToken cancellationToken)
    {
        var result = await _identityService.BuildPasswordResetChangedAsync(notification.UserId, cancellationToken);
        if (result.IsError)
        {
            _logger.LogError("Password changed email could not be built for user {UserId}: {ErrorCode}", notification.UserId, result.TopError.Code);
            return;
        }
        await _emailQueue.EnqueueAsync(result.Value, cancellationToken);
        _logger.LogInformation("Password changed email queued for user {UserId}", notification.UserId);
    }
}