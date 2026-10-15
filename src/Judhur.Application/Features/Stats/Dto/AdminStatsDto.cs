using Judhur.Application.Features.Users.Dto;

namespace Judhur.Application.Features.Stats.Dto;

public sealed record AdminStatsDto(
    UserStatsDto Users,
    PropertyStatsDto Properties,
    int PendingReports,
    ActivityStatsDto LastSevenDays);

public sealed record PropertyStatsDto(
    int Published,
    int WaitingForReview,
    int Incomplete,
    int Rejected,
    int Inactive,
    int Sold,
    int Rented);

public sealed record ActivityStatsDto(int NewUsers, int NewListings, int Messages, int Reviews, int Reports);
