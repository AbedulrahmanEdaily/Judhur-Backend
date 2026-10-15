using Judhur.Application.Common.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Events;

public sealed class SendEmailChangeConfirmationHandler(IIdentityService identityService, IEmailQueue emailQueue, ILogger<SendEmailChangeConfirmationHandler> logger)
    : INotificationHandler<EmailChangeRequested>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailQueue _emailQueue = emailQueue;
    private readonly ILogger<SendEmailChangeConfirmationHandler> _logger = logger;

    public async Task Handle(EmailChangeRequested notification, CancellationToken cancellationToken)
    {
        var result = await _identityService.BuildEmailChangeEmailAsync(notification.UserId, notification.NewEmail, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Email change confirmation could not be built for user {UserId}: {ErrorCode}", notification.UserId, result.TopError.Code);
            return;
        }
        await _emailQueue.EnqueueAsync(result.Value, cancellationToken);
        _logger.LogInformation("Email change confirmation queued for user {UserId}", notification.UserId);
    }
}
