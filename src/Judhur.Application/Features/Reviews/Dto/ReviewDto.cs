namespace Judhur.Application.Features.Reviews.Dto;

public sealed record ReviewDto(
    Guid Id,
    Guid SellerId,
    decimal Rating,
    string? Comment,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastModifiedUtc);
