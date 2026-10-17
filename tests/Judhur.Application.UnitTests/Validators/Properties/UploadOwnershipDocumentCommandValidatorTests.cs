using FluentValidation.TestHelper;

using Judhur.Application.Features.Properties.Commands.UploadOwnershipDocument;

namespace Judhur.Application.UnitTests.Validators.Properties;

public sealed class UploadOwnershipDocumentCommandValidatorTests
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private const string UnsupportedFormat = "صيغة الوثيقة غير مدعومة، الصيغ المسموحة: PDF و JPG و PNG و WEBP.";

    private static readonly UploadOwnershipDocumentCommand Valid = new(Guid.NewGuid(), Stream.Null, "deed.pdf", "application/pdf", 4096);

    private readonly UploadOwnershipDocumentCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyPropertyId_ReturnsError()
    {
        _validator.TestValidate(Valid with { PropertyId = Guid.Empty }).ShouldHaveValidationErrorFor(c => c.PropertyId);
    }

    [Theory]
    [InlineData("deed.pdf")]
    [InlineData("deed.PDF")]
    [InlineData("scan.jpg")]
    [InlineData("scan.jpeg")]
    [InlineData("scan.png")]
    [InlineData("scan.WebP")]
    [InlineData("صك الملكية.pdf")]
    public void Validate_AllowedExtension_HasNoError(string fileName)
    {
        _validator.TestValidate(Valid with { FileName = fileName }).ShouldNotHaveValidationErrorFor(c => c.FileName);
    }

    [Theory]
    [InlineData("deed")]
    [InlineData("deed.docx")]
    [InlineData("deed.pdf.exe")]
    [InlineData("deed.gif")]
    [InlineData("")]
    public void Validate_UnsupportedExtension_ReturnsError(string fileName)
    {
        _validator.TestValidate(Valid with { FileName = fileName })
            .ShouldHaveValidationErrorFor(c => c.FileName)
            .WithErrorMessage(UnsupportedFormat);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void Validate_AllowedContentType_HasNoError(string contentType)
    {
        _validator.TestValidate(Valid with { ContentType = contentType }).ShouldNotHaveValidationErrorFor(c => c.ContentType);
    }

    [Theory]
    [InlineData("application/msword")]
    [InlineData("text/plain")]
    [InlineData("")]
    public void Validate_UnsupportedContentType_ReturnsError(string contentType)
    {
        _validator.TestValidate(Valid with { ContentType = contentType })
            .ShouldHaveValidationErrorFor(c => c.ContentType)
            .WithErrorMessage(UnsupportedFormat);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_EmptyFile_ReturnsError(long length)
    {
        _validator.TestValidate(Valid with { Length = length })
            .ShouldHaveValidationErrorFor(c => c.Length)
            .WithErrorMessage("الملف فارغ.");
    }

    [Fact]
    public void Validate_LengthAtLimit_HasNoError()
    {
        _validator.TestValidate(Valid with { Length = MaxBytes }).ShouldNotHaveValidationErrorFor(c => c.Length);
    }

    [Fact]
    public void Validate_LengthOverLimit_ReturnsError()
    {
        _validator.TestValidate(Valid with { Length = MaxBytes + 1 })
            .ShouldHaveValidationErrorFor(c => c.Length)
            .WithErrorMessage("يجب ألا يتجاوز حجم وثيقة الملكية 10 ميغابايت.");
    }
}
