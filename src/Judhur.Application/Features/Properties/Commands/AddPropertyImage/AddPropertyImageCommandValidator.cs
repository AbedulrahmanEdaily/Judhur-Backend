using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.AddPropertyImage;

public sealed class AddPropertyImageCommandValidator : AbstractValidator<AddPropertyImageCommand>
{
    private const long MaxFileSizeInBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public AddPropertyImageCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");

        RuleFor(p => p.Length)
            .GreaterThan(0).WithMessage("الملف فارغ.")
            .LessThanOrEqualTo(MaxFileSizeInBytes).WithMessage("يجب ألا يتجاوز حجم الصورة 5 ميغابايت.");

        RuleFor(p => p.ContentType)
            .Must(type => AllowedContentTypes.Contains(type))
            .WithMessage("صيغة الصورة غير مدعومة، الصيغ المسموحة: JPG و PNG و WEBP.");
    }
}