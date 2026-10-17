using Judhur.Domain.Common.Results;
using Judhur.Domain.Reviews;

namespace Judhur.Tests.Common.Factories;

public static class ReviewFactory
{
    public const decimal DefaultRating = 4.5m;
    public const string DefaultComment = "بائع متعاون وصادق";

    public static readonly Guid DefaultReviewerId = ConversationFactory.DefaultBuyerId;
    public static readonly Guid DefaultSellerId = PropertyFactory.DefaultSellerId;

    public static Result<Review> CreateResult(
        Guid? reviewerId = null,
        Guid? sellerId = null,
        decimal rating = DefaultRating,
        string? comment = DefaultComment,
        Guid? id = null)
        => Review.Create(
            id ?? Guid.NewGuid(),
            reviewerId ?? DefaultReviewerId,
            sellerId ?? DefaultSellerId,
            rating,
            comment);

    public static Review Create(
        Guid? reviewerId = null,
        Guid? sellerId = null,
        decimal rating = DefaultRating,
        string? comment = DefaultComment,
        Guid? id = null)
        => CreateResult(reviewerId, sellerId, rating, comment, id).GetValueOrThrow();
}
