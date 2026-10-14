using MediatR;

namespace Judhur.Application.Features.Conversations.Events;

public sealed record MessageSent(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderId,
    Guid RecipientId,
    string Content,
    DateTimeOffset SentAtUtc) : INotification;
