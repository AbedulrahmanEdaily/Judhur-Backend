using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.UploadProfileImage;

public sealed class UploadProfileImageCommandValidator : AbstractValidator<UploadProfileImageCommand>
{
    private const long MaxFileSizeInBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public UploadProfileImageCommandValidator()
    {
        RuleFor(p => p.Length)
            .GreaterThan(0).WithMessage("الملف فارغ.")
            .LessThanOrEqualTo(MaxFileSizeInBytes).WithMessage("يجب ألا يتجاوز حجم الصورة 5 ميغابايت.");

        RuleFor(p => p.ContentType)
            .Must(type => AllowedContentTypes.Contains(type))
            .WithMessage("صيغة الصورة غير مدعومة، الصيغ المسموحة: JPG و PNG و WEBP.");
    }
}
