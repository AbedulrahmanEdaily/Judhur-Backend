namespace Judhur.Application.Features.Users.Dto;

public sealed record AdminUserDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string City,
    string? ProfileImageUrl,
    bool IsAdmin,
    bool EmailConfirmed,
    DateTimeOffset? BannedAtUtc,
    string? BanReason,
    DateTimeOffset CreatedAtUtc,
    int PropertiesCount,
    int ActiveListingsCount,
    int PendingReportsOnListings);
