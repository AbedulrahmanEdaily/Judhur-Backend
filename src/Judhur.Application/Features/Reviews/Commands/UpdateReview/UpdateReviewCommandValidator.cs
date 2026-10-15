using FluentValidation;

using Judhur.Domain.Reviews;

namespace Judhur.Application.Features.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(r => r.ReviewId)
            .NotEmpty().WithMessage("معرّف التقييم مطلوب.");

        RuleFor(r => r.Rating)
            .InclusiveBetween(Review.MinRating, Review.MaxRating)
            .WithMessage("التقييم يجب أن يكون من 0.5 إلى 5.")
            .Must(rating => rating % Review.RatingStep == 0)
            .WithMessage("التقييم يجب أن يكون بخطوات 0.5.");

        RuleFor(r => r.Comment)
            .Must(comment => comment is null || comment.Trim().Length <= Review.MaxCommentLength)
            .WithMessage($"لا يمكن أن يتجاوز التعليق {Review.MaxCommentLength} حرف.");
    }
}
