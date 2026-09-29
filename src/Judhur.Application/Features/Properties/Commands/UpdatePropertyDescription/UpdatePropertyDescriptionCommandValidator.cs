using FluentValidation;

using Judhur.Domain.Properties;

namespace Judhur.Application.Features.Properties.Commands.UpdatePropertyDescription;

public sealed class UpdatePropertyDescriptionCommandValidator : AbstractValidator<UpdatePropertyDescriptionCommand>
{
    public UpdatePropertyDescriptionCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");

        RuleFor(p => p.Description)
            .MaximumLength(Property.MaxDescriptionLength)
            .WithMessage($"لا يمكن أن يتجاوز الوصف {Property.MaxDescriptionLength} حرف.");
    }
}