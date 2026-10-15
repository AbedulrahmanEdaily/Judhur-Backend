using Judhur.Application.Common.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Events;

public sealed class SendEmailChangedNoticeHandler(IIdentityService identityService, IEmailQueue emailQueue, ILogger<SendEmailChangedNoticeHandler> logger)
    : INotificationHandler<EmailChanged>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailQueue _emailQueue = emailQueue;
    private readonly ILogger<SendEmailChangedNoticeHandler> _logger = logger;

    public async Task Handle(EmailChanged notification, CancellationToken cancellationToken)
    {
        var result = await _identityService.BuildEmailChangedNoticeAsync(notification.UserId, notification.OldEmail, cancellationToken);
        if (result.IsError)
        {
            _logger.LogError("Email changed notice could not be built for user {UserId}: {ErrorCode}", notification.UserId, result.TopError.Code);
            return;
        }
        await _emailQueue.EnqueueAsync(result.Value, cancellationToken);
        _logger.LogInformation("Email changed notice queued for the previous address of user {UserId}", notification.UserId);
    }
}
