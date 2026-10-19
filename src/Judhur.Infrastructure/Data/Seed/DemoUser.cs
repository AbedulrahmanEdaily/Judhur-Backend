namespace Judhur.Infrastructure.Data.Seed;

internal sealed record DemoUser(
    string Key,
    string Email,
    string FullName,
    string City,
    string? PhoneNumber,
    string? Bio,
    int JoinedDaysAgo);
