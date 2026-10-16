using System.Globalization;
using Judhur.Domain.Reviews;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Reviews;

public sealed class ReviewCreateTests
{
    private static decimal Dec(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [Fact]
    public void Create_ValidReview_ReturnsSuccess()
    {
        var result = ReviewFactory.CreateResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ValidReview_CopiesData()
    {
        var reviewerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        var review = ReviewFactory.Create(reviewerId, sellerId, 3.5m, "تعامل ممتاز");

        Assert.Equal(reviewerId, review.ReviewerId);
        Assert.Equal(sellerId, review.SellerId);
        Assert.Equal(3.5m, review.Rating);
        Assert.Equal("تعامل ممتاز", review.Comment);
    }

    [Fact]
    public void Create_GivenId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var review = ReviewFactory.Create(id: id);

        Assert.Equal(id, review.Id);
    }

    [Fact]
    public void Create_EmptyId_GeneratesNewId()
    {
        var review = ReviewFactory.Create(id: Guid.Empty);

        Assert.NotEqual(Guid.Empty, review.Id);
    }

    [Fact]
    public void Create_EmptyReviewer_ReturnsReviewerIdRequired()
    {
        var result = ReviewFactory.CreateResult(reviewerId: Guid.Empty);

        Assert.Equal(ReviewErrors.ReviewerIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptySeller_ReturnsSellerIdRequired()
    {
        var result = ReviewFactory.CreateResult(sellerId: Guid.Empty);

        Assert.Equal(ReviewErrors.SellerIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptyReviewerAndSeller_ReturnsReviewerIdRequiredFirst()
    {
        var result = ReviewFactory.CreateResult(Guid.Empty, Guid.Empty);

        Assert.Equal(ReviewErrors.ReviewerIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ReviewerIsSeller_ReturnsCannotReviewSelf()
    {
        var userId = Guid.NewGuid();

        var result = ReviewFactory.CreateResult(userId, userId);

        Assert.Equal(ReviewErrors.CannotReviewSelf.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ReviewerIsSellerWithInvalidRating_ReturnsCannotReviewSelfFirst()
    {
        var userId = Guid.NewGuid();

        var result = ReviewFactory.CreateResult(userId, userId, 7m);

        Assert.Equal(ReviewErrors.CannotReviewSelf.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("0.5")]
    [InlineData("1")]
    [InlineData("2.5")]
    [InlineData("4.50")]
    [InlineData("5")]
    [InlineData("5.0")]
    public void Create_RatingOnHalfStepWithinRange_ReturnsSuccess(string rating)
    {
        var result = ReviewFactory.CreateResult(rating: Dec(rating));

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.5")]
    [InlineData("0.49")]
    [InlineData("5.5")]
    [InlineData("10")]
    [InlineData("0.25")]
    [InlineData("3.3")]
    [InlineData("4.99")]
    public void Create_RatingOffStepOrOutOfRange_ReturnsInvalidRating(string rating)
    {
        var result = ReviewFactory.CreateResult(rating: Dec(rating));

        Assert.Equal(ReviewErrors.InvalidRating.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankComment_StoresNullComment(string? comment)
    {
        var review = ReviewFactory.Create(comment: comment);

        Assert.Null(review.Comment);
    }

    [Fact]
    public void Create_CommentWithSurroundingWhitespace_StoresTrimmedComment()
    {
        var review = ReviewFactory.Create(comment: $"  {ReviewFactory.DefaultComment}\t");

        Assert.Equal(ReviewFactory.DefaultComment, review.Comment);
    }

    [Fact]
    public void Create_CommentAtLimit_ReturnsSuccess()
    {
        var result = ReviewFactory.CreateResult(comment: new string('ك', Review.MaxCommentLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_CommentAtLimitAfterTrimming_ReturnsSuccess()
    {
        var comment = new string('ك', Review.MaxCommentLength);

        var review = ReviewFactory.Create(comment: $"   {comment}   ");

        Assert.Equal(comment, review.Comment);
    }

    [Fact]
    public void Create_CommentOverLimit_ReturnsCommentTooLong()
    {
        var result = ReviewFactory.CreateResult(comment: new string('ك', Review.MaxCommentLength + 1));

        Assert.Equal(ReviewErrors.CommentTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_InvalidRatingAndCommentOverLimit_ReturnsInvalidRatingFirst()
    {
        var result = ReviewFactory.CreateResult(rating: 0m, comment: new string('ك', Review.MaxCommentLength + 1));

        Assert.Equal(ReviewErrors.InvalidRating.Code, result.TopError.Code);
    }
}
