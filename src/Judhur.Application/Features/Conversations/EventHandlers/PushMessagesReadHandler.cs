using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Conversations.Events;

using MediatR;

namespace Judhur.Application.Features.Conversations.EventHandlers;

public sealed class PushMessagesReadHandler(IRealtimeNotifier realtimeNotifier) : INotificationHandler<MessagesRead>
{
    private readonly IRealtimeNotifier _realtimeNotifier = realtimeNotifier;

    public Task Handle(MessagesRead notification, CancellationToken cancellationToken)
        => _realtimeNotifier.MessagesReadAsync(
            notification.OtherParticipantId,
            notification.ConversationId,
            notification.ReadAtUtc,
            cancellationToken);
}
