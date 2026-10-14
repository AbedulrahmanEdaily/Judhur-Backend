namespace Judhur.Application.Features.Conversations.Dto;

public sealed record MessagesPageDto(IReadOnlyList<MessageDto> Items, bool HasMore);
