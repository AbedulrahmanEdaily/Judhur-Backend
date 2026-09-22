using Judhur.Application.Common.Interfaces;

using MediatR;

namespace Judhur.Application.Features.Identity.Events;

public sealed class SendPasswordResetEmailHandler(IIdentityService identityService, IEmailQueue emailQueue)
    : INotificationHandler<PasswordResetRequested>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailQueue _emailQueue = emailQueue;

    public async Task Handle(PasswordResetRequested notification, CancellationToken cancellationToken)
    {
        var result = await _identityService.BuildPasswordResetEmailAsync(notification.UserId, cancellationToken);
        if (result.IsError)
        {
            return;
        }
        await _emailQueue.EnqueueAsync(result.Value, cancellationToken);
    }
}