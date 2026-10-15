namespace Judhur.Application.Features.Users.Dto;

public sealed record UserStatsDto(int TotalUsers, int NewUsers, int BannedUsers, int UnconfirmedUsers);
