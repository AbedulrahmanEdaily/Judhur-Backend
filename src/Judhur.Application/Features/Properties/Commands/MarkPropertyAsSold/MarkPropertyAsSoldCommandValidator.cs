using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.MarkPropertyAsSold;

public sealed class MarkPropertyAsSoldCommandValidator : AbstractValidator<MarkPropertyAsSoldCommand>
{
    public MarkPropertyAsSoldCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");
    }
}
