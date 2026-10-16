using System.Globalization;

using FluentValidation.TestHelper;

using Judhur.Application.Features.Reviews.Commands.CreateReview;
using Judhur.Domain.Reviews;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Reviews;

public sealed class CreateReviewCommandValidatorTests
{
    private static readonly CreateReviewCommand Valid = new(Guid.NewGuid(), 4.5m, "بائع محترم");

    private readonly CreateReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullComment_HasNoErrors()
    {
        _validator.TestValidate(Valid with { Comment = null }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptySellerId_ReturnsError()
    {
        _validator.TestValidate(Valid with { SellerId = Guid.Empty })
            .ShouldHaveValidationErrorFor(c => c.SellerId)
            .WithErrorMessage("معرّف البائع مطلوب.");
    }

    [Theory]
    [InlineData("0.5")]
    [InlineData("1")]
    [InlineData("2.5")]
    [InlineData("5")]
    [InlineData("5.0")]
    public void Validate_RatingOnHalfStep_HasNoError(string rating)
    {
        _validator.TestValidate(Valid with { Rating = decimal.Parse(rating, CultureInfo.InvariantCulture) })
            .ShouldNotHaveValidationErrorFor(c => c.Rating);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.5")]
    [InlineData("5.5")]
    public void Validate_RatingOutOfRange_ReturnsRangeError(string rating)
    {
        _validator.TestValidate(Valid with { Rating = decimal.Parse(rating, CultureInfo.InvariantCulture) })
            .ShouldHaveValidationErrorFor(c => c.Rating)
            .WithErrorMessage("التقييم يجب أن يكون من 0.5 إلى 5.");
    }

    [Theory]
    [InlineData("0.75")]
    [InlineData("3.3")]
    [InlineData("4.25")]
    public void Validate_RatingOffStep_ReturnsStepError(string rating)
    {
        _validator.TestValidate(Valid with { Rating = decimal.Parse(rating, CultureInfo.InvariantCulture) })
            .ShouldHaveValidationErrorFor(c => c.Rating)
            .WithErrorMessage("التقييم يجب أن يكون بخطوات 0.5.");
    }

    [Fact]
    public void Validate_CommentAtMaxLengthAfterTrim_HasNoError()
    {
        _validator.TestValidate(Valid with { Comment = $"  {Text(Review.MaxCommentLength)}  " })
            .ShouldNotHaveValidationErrorFor(c => c.Comment);
    }

    [Fact]
    public void Validate_CommentOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { Comment = Text(Review.MaxCommentLength + 1) })
            .ShouldHaveValidationErrorFor(c => c.Comment)
            .WithErrorMessage($"لا يمكن أن يتجاوز التعليق {Review.MaxCommentLength} حرف.");
    }
}
