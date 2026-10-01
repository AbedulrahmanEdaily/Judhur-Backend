namespace Judhur.Application.Features.Sellers.Dto;

public sealed record SellerProfileDto(
    Guid Id,
    string FullName,
    string? ProfileImageUrl,
    string City,
    string? Bio,
    DateTimeOffset MemberSinceUtc,
    int ActiveListingsCount);
