namespace Judhur.Application.Features.Conversations.Dto;

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Content,
    DateTimeOffset SentAtUtc,
    DateTimeOffset? ReadAtUtc,
    bool IsMine);
