namespace Judhur.Application.Common.Models;

public sealed record SellerInfo(
    Guid Id,
    string FullName,
    string? ProfileImageUrl,
    string City,
    string? Bio,
    DateTimeOffset MemberSinceUtc);
