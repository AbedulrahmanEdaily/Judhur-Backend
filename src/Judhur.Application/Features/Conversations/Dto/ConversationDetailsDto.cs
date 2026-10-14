namespace Judhur.Application.Features.Conversations.Dto;

public sealed record ConversationDetailsDto(
    Guid Id,
    ConversationPropertyDto Property,
    ChatParticipantDto? OtherParty,
    ConversationRole MyRole,
    DateTimeOffset CreatedAtUtc);
