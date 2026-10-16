using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.UploadProfileImage;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class UploadProfileImageCommandValidatorTests
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly UploadProfileImageCommand Valid = new(Stream.Null, "me.jpg", "image/jpeg", 1024);

    private readonly UploadProfileImageCommandValidator _validator = new();

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void Validate_AllowedContentType_HasNoErrors(string contentType)
    {
        _validator.TestValidate(Valid with { ContentType = contentType }).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("IMAGE/JPEG")]
    [InlineData("")]
    public void Validate_UnsupportedContentType_ReturnsError(string contentType)
    {
        _validator.TestValidate(Valid with { ContentType = contentType })
            .ShouldHaveValidationErrorFor(c => c.ContentType)
            .WithErrorMessage("صيغة الصورة غير مدعومة، الصيغ المسموحة: JPG و PNG و WEBP.");
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

    [Theory]
    [InlineData(1)]
    [InlineData(MaxBytes)]
    public void Validate_LengthWithinLimit_HasNoError(long length)
    {
        _validator.TestValidate(Valid with { Length = length }).ShouldNotHaveValidationErrorFor(c => c.Length);
    }

    [Fact]
    public void Validate_LengthOverLimit_ReturnsError()
    {
        _validator.TestValidate(Valid with { Length = MaxBytes + 1 })
            .ShouldHaveValidationErrorFor(c => c.Length)
            .WithErrorMessage("يجب ألا يتجاوز حجم الصورة 5 ميغابايت.");
    }
}
