using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.ReactivateProperty;

public sealed class ReactivatePropertyCommandValidator : AbstractValidator<ReactivatePropertyCommand>
{
    public ReactivatePropertyCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");
    }
}
