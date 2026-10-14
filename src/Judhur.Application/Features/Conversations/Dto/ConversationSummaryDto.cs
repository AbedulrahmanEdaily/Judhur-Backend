namespace Judhur.Application.Features.Conversations.Dto;

public sealed record ConversationSummaryDto(
    Guid Id,
    ConversationPropertyDto Property,
    ChatParticipantDto? OtherParty,
    ConversationRole MyRole,
    string? LastMessagePreview,
    bool LastMessageIsMine,
    DateTimeOffset? LastMessageAtUtc,
    int UnreadCount);
