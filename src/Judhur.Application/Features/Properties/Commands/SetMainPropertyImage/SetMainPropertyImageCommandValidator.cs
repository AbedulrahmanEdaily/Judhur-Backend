using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.SetMainPropertyImage;

public sealed class SetMainPropertyImageCommandValidator : AbstractValidator<SetMainPropertyImageCommand>
{
    public SetMainPropertyImageCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");

        RuleFor(p => p.ImageId)
            .NotEmpty().WithMessage("معرّف الصورة مطلوب.");
    }
}