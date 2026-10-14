namespace Judhur.Application.Features.Conversations.Dto;

public sealed record ConversationPropertyDto(Guid Id, string Title, string? MainImageUrl, bool IsAvailable);
