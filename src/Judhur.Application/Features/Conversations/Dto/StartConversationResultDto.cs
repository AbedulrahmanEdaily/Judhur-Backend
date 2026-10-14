namespace Judhur.Application.Features.Conversations.Dto;

public sealed record StartConversationResultDto(Guid ConversationId, bool IsNew, MessageDto Message);
