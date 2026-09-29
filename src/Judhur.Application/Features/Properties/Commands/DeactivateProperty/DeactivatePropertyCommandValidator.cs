using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.DeactivateProperty;

public sealed class DeactivatePropertyCommandValidator : AbstractValidator<DeactivatePropertyCommand>
{
    public DeactivatePropertyCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");
    }
}
