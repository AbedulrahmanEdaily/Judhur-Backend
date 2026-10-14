namespace Judhur.Application.Features.Conversations.Dto;

public sealed record ChatParticipantDto(Guid Id, string FullName, string? ProfileImageUrl);
