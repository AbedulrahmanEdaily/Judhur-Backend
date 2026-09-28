using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.DeletePropertyImage;

public sealed class DeletePropertyImageCommandValidator : AbstractValidator<DeletePropertyImageCommand>
{
    public DeletePropertyImageCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");

        RuleFor(p => p.ImageId)
            .NotEmpty().WithMessage("معرّف الصورة مطلوب.");
    }
}