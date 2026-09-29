using FluentValidation;

namespace Judhur.Application.Features.Properties.Commands.UploadOwnershipDocument;

public sealed class UploadOwnershipDocumentCommandValidator : AbstractValidator<UploadOwnershipDocumentCommand>
{
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024;
    private const string UnsupportedFormatMessage = "صيغة الوثيقة غير مدعومة، الصيغ المسموحة: PDF و JPG و PNG و WEBP.";
    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png", "image/webp"];

    public UploadOwnershipDocumentCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");

        RuleFor(p => p.Length)
            .GreaterThan(0).WithMessage("الملف فارغ.")
            .LessThanOrEqualTo(MaxFileSizeInBytes).WithMessage("يجب ألا يتجاوز حجم وثيقة الملكية 10 ميغابايت.");

        RuleFor(p => p.FileName)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage(UnsupportedFormatMessage);

        RuleFor(p => p.ContentType)
            .Must(type => AllowedContentTypes.Contains(type))
            .WithMessage(UnsupportedFormatMessage);
    }
}
