using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Reviews;

public sealed class Review : AuditableEntity
{
    public const decimal MinRating = 0.5m;
    public const decimal MaxRating = 5.0m;
    public const decimal RatingStep = 0.5m;
    public Guid ReviewerId { get; private set; }
    public Guid SellerId { get; private set; }
    public decimal Rating { get; private set; }
    public string? Comment { get; private set; }

    private Review() { }
    private Review(Guid id, Guid reviewerId, Guid sellerId, decimal rating, string? comment) : base(id)
    {
        ReviewerId = reviewerId;
        SellerId = sellerId;
        Rating = rating;
        Comment = comment;
    }
    private static bool IsValidRating(decimal rating)
        => rating >= MinRating && rating <= MaxRating && rating % RatingStep == 0;
    public static Result<Review> Create(Guid id, Guid reviewerId, Guid sellerId, decimal rating, string? comment)
    {
        if (reviewerId == Guid.Empty)
        {
            return ReviewErrors.ReviewerIdRequired;
        }
        if (sellerId == Guid.Empty)
        {
            return ReviewErrors.SellerIdRequired;
        }
        if (sellerId == reviewerId)
        {
            return ReviewErrors.CannotReviewSelf;
        }
        if (!IsValidRating(rating))
        {
            return ReviewErrors.InvalidRating;
        }
        return new Review(id, reviewerId, sellerId, rating, comment);
    }
    public Result<Updated> Update(decimal rating, string? comment)
    {
        if (!IsValidRating(rating))
        {
            return ReviewErrors.InvalidRating;
        }
        Rating = rating;
        Comment = comment;
        return Result.Updated;
    }
}