namespace Judhur.Application.Features.Identity.Dtos;

public sealed record MyProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string City,
    string? Bio,
    string? ProfileImageUrl,
    IReadOnlyList<string> Roles,
    bool HasPassword,
    DateTimeOffset CreatedAtUtc);
