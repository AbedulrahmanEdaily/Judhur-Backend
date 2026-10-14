using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Application.Features.Conversations.Events;

using MediatR;

namespace Judhur.Application.Features.Conversations.EventHandlers;

public sealed class PushMessageSentHandler(IRealtimeNotifier realtimeNotifier) : INotificationHandler<MessageSent>
{
    private readonly IRealtimeNotifier _realtimeNotifier = realtimeNotifier;

    public Task Handle(MessageSent notification, CancellationToken cancellationToken)
        => _realtimeNotifier.MessageReceivedAsync(
            notification.RecipientId,
            new MessageDto(
                notification.MessageId,
                notification.ConversationId,
                notification.SenderId,
                notification.Content,
                notification.SentAtUtc,
                null,
                false),
            cancellationToken);
}
