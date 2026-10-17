using System.Globalization;

using FluentValidation.TestHelper;

using Judhur.Application.Features.Reviews.Commands.UpdateReview;
using Judhur.Domain.Reviews;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Reviews;

public sealed class UpdateReviewCommandValidatorTests
{
    private static readonly UpdateReviewCommand Valid = new(Guid.NewGuid(), 3m, null);

    private readonly UpdateReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyReviewId_ReturnsError()
    {
        _validator.TestValidate(Valid with { ReviewId = Guid.Empty })
            .ShouldHaveValidationErrorFor(c => c.ReviewId)
            .WithErrorMessage("معرّف التقييم مطلوب.");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("5.5")]
    public void Validate_RatingOutOfRange_ReturnsError(string rating)
    {
        _validator.TestValidate(Valid with { Rating = decimal.Parse(rating, CultureInfo.InvariantCulture) })
            .ShouldHaveValidationErrorFor(c => c.Rating)
            .WithErrorMessage("التقييم يجب أن يكون من 0.5 إلى 5.");
    }

    [Fact]
    public void Validate_RatingOffStep_ReturnsError()
    {
        _validator.TestValidate(Valid with { Rating = 2.2m })
            .ShouldHaveValidationErrorFor(c => c.Rating)
            .WithErrorMessage("التقييم يجب أن يكون بخطوات 0.5.");
    }

    [Fact]
    public void Validate_CommentAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { Comment = Text(Review.MaxCommentLength) }).ShouldNotHaveValidationErrorFor(c => c.Comment);
    }

    [Fact]
    public void Validate_CommentOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { Comment = Text(Review.MaxCommentLength + 1) }).ShouldHaveValidationErrorFor(c => c.Comment);
    }
}
