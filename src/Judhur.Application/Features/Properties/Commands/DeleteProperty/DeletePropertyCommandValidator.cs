using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.DeleteProperty;

public sealed class DeletePropertyCommandValidator : AbstractValidator<DeletePropertyCommand>
{
    public DeletePropertyCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرف العقار مطلوب.");
    }
}