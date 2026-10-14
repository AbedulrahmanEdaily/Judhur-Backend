using MediatR;

namespace Judhur.Application.Features.Conversations.Events;

public sealed record MessagesRead(Guid ConversationId, Guid ReaderId, Guid OtherParticipantId, DateTimeOffset ReadAtUtc) : INotification;
