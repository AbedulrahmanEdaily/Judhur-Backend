using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.ApproveProperty;

public sealed class ApprovePropertyCommandValidator : AbstractValidator<ApprovePropertyCommand>
{
    public ApprovePropertyCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");
    }
}