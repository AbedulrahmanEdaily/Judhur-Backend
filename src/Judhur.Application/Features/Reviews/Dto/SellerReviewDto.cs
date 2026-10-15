namespace Judhur.Application.Features.Reviews.Dto;

public sealed record SellerReviewDto(
    Guid Id,
    decimal Rating,
    string? Comment,
    ReviewerDto? Reviewer,
    DateTimeOffset CreatedAtUtc,
    bool IsEdited);
