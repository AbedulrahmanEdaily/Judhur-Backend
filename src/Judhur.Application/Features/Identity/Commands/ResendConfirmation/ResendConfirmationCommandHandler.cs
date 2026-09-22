using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;


using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ResendConfirmation;

public sealed class SendConfirmationEmailHandler(IIdentityService identityService, IEmailQueue emailQueue)
    : INotificationHandler<UserRegistered>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailQueue _emailQueue = emailQueue;

    public async Task Handle(UserRegistered notification, CancellationToken cancellationToken)
    {
        var result = await _identityService.BuildConfirmationEmailAsync(notification.UserId, cancellationToken);
        if (result.IsError)
        {
            return;
        }
        await _emailQueue.EnqueueAsync(result.Value, cancellationToken);
    }
}
