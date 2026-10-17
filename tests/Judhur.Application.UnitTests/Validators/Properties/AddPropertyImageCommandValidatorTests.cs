using FluentValidation.TestHelper;

using Judhur.Application.Features.Properties.Commands.AddPropertyImage;

namespace Judhur.Application.UnitTests.Validators.Properties;

public sealed class AddPropertyImageCommandValidatorTests
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly AddPropertyImageCommand Valid = new(Guid.NewGuid(), Stream.Null, "front.png", "image/png", 2048, false);

    private readonly AddPropertyImageCommandValidator _validator = new();

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void Validate_AllowedContentType_HasNoErrors(string contentType)
    {
        _validator.TestValidate(Valid with { ContentType = contentType }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyPropertyId_ReturnsError()
    {
        _validator.TestValidate(Valid with { PropertyId = Guid.Empty })
            .ShouldHaveValidationErrorFor(c => c.PropertyId)
            .WithErrorMessage("معرّف العقار مطلوب.");
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("image/svg+xml")]
    [InlineData("application/pdf")]
    [InlineData("")]
    public void Validate_UnsupportedContentType_ReturnsError(string contentType)
    {
        _validator.TestValidate(Valid with { ContentType = contentType })
            .ShouldHaveValidationErrorFor(c => c.ContentType)
            .WithErrorMessage("صيغة الصورة غير مدعومة، الصيغ المسموحة: JPG و PNG و WEBP.");
    }

    [Fact]
    public void Validate_EmptyFile_ReturnsError()
    {
        _validator.TestValidate(Valid with { Length = 0 })
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
            .WithErrorMessage("يجب ألا يتجاوز حجم الصورة 5 ميغابايت.");
    }
}
