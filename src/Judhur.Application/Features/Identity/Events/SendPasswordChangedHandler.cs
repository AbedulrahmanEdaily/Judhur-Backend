using Judhur.Application.Common.Interfaces;

using MediatR;

namespace Judhur.Application.Features.Identity.Events;

public sealed class SendPasswordChangedHandler(IIdentityService identityService, IEmailQueue emailQueue) : INotificationHandler<PasswordChanged>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailQueue _emailQueue = emailQueue;

    public async Task Handle(PasswordChanged notification, CancellationToken cancellationToken)
    {
        var result = await _identityService.BuildPasswordResetChangedAsync(notification.UserId, cancellationToken);
        if (result.IsError)
        {
            return;
        }
        await _emailQueue.EnqueueAsync(result.Value, cancellationToken);
    }
}