using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.ResubmitPropertyForReview;

public sealed class ResubmitPropertyForReviewCommandValidator : AbstractValidator<ResubmitPropertyForReviewCommand>
{
    public ResubmitPropertyForReviewCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");
    }
}
