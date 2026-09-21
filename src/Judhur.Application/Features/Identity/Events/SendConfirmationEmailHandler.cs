using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;

using MediatR;

namespace Judhur.Application.Features.Identity.Events;

public sealed class SendConfirmationEmailHandler(IEmailQueue emailQueue) : INotificationHandler<UserRegistered>
{
    private readonly IEmailQueue _emailQueue = emailQueue;

    public Task Handle(UserRegistered notification, CancellationToken cancellationToken)
    {
        return _emailQueue.EnqueueAsync(new EmailConfirmationRequest(notification.UserId), cancellationToken).AsTask();
    }
}