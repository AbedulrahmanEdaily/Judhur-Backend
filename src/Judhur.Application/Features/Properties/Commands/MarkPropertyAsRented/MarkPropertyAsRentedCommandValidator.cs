using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.MarkPropertyAsRented;

public sealed class MarkPropertyAsRentedCommandValidator : AbstractValidator<MarkPropertyAsRentedCommand>
{
    public MarkPropertyAsRentedCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");
    }
}
