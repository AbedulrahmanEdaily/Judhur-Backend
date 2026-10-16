using System.Globalization;
using Judhur.Domain.Reviews;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Reviews;

public sealed class ReviewUpdateTests
{
    private static decimal Dec(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [Fact]
    public void Update_ValidRatingAndComment_ReplacesThem()
    {
        var review = ReviewFactory.Create();

        var result = review.Update(2m, "تأخر في الرد");

        Assert.True(result.IsSuccess);
        Assert.Equal(2m, review.Rating);
        Assert.Equal("تأخر في الرد", review.Comment);
    }

    [Fact]
    public void Update_ValidData_KeepsReviewerAndSeller()
    {
        var review = ReviewFactory.Create();

        review.Update(1.5m, null);

        Assert.Equal(ReviewFactory.DefaultReviewerId, review.ReviewerId);
        Assert.Equal(ReviewFactory.DefaultSellerId, review.SellerId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Update_BlankComment_ClearsComment(string? comment)
    {
        var review = ReviewFactory.Create();

        review.Update(ReviewFactory.DefaultRating, comment);

        Assert.Null(review.Comment);
    }

    [Fact]
    public void Update_CommentWithSurroundingWhitespace_StoresTrimmedComment()
    {
        var review = ReviewFactory.Create();

        review.Update(ReviewFactory.DefaultRating, "\n بائع رائع \n");

        Assert.Equal("بائع رائع", review.Comment);
    }

    [Theory]
    [InlineData("0.5")]
    [InlineData("5")]
    public void Update_BoundaryRating_ReturnsSuccess(string rating)
    {
        var review = ReviewFactory.Create();

        var result = review.Update(Dec(rating), null);

        Assert.True(result.IsSuccess);
        Assert.Equal(Dec(rating), review.Rating);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("5.5")]
    [InlineData("2.25")]
    public void Update_InvalidRating_ReturnsInvalidRating(string rating)
    {
        var review = ReviewFactory.Create();

        var result = review.Update(Dec(rating), "تعليق");

        Assert.Equal(ReviewErrors.InvalidRating.Code, result.TopError.Code);
    }

    [Fact]
    public void Update_InvalidRating_KeepsOldRatingAndComment()
    {
        var review = ReviewFactory.Create();

        review.Update(0m, "تعليق جديد");

        Assert.Equal(ReviewFactory.DefaultRating, review.Rating);
        Assert.Equal(ReviewFactory.DefaultComment, review.Comment);
    }

    [Fact]
    public void Update_CommentAtLimit_ReturnsSuccess()
    {
        var review = ReviewFactory.Create();

        var result = review.Update(3m, new string('ك', Review.MaxCommentLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Update_CommentOverLimit_ReturnsCommentTooLong()
    {
        var review = ReviewFactory.Create();

        var result = review.Update(3m, new string('ك', Review.MaxCommentLength + 1));

        Assert.Equal(ReviewErrors.CommentTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void Update_CommentOverLimit_KeepsOldRatingAndComment()
    {
        var review = ReviewFactory.Create();

        review.Update(3m, new string('ك', Review.MaxCommentLength + 1));

        Assert.Equal(ReviewFactory.DefaultRating, review.Rating);
        Assert.Equal(ReviewFactory.DefaultComment, review.Comment);
    }
}
