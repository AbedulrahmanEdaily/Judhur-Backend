using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.GoogleLogin;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class GoogleLoginCommandValidatorTests
{
    private static readonly GoogleLoginCommand Valid = new("google-id-token", null, null);

    private readonly GoogleLoginCommandValidator _validator = new();

    [Fact]
    public void Validate_TokenOnly_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TokenWithPhoneAndCity_HasNoErrors()
    {
        _validator.TestValidate(Valid with { PhoneNumber = ValidPhone, City = "الخليل" }).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_BlankIdToken_ReturnsError(string idToken)
    {
        _validator.TestValidate(Valid with { IdToken = idToken })
            .ShouldHaveValidationErrorFor(c => c.IdToken)
            .WithErrorMessage("رمز Google مطلوب.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingPhone_HasNoError(string? phone)
    {
        _validator.TestValidate(Valid with { PhoneNumber = phone }).ShouldNotHaveValidationErrorFor(c => c.PhoneNumber);
    }

    [Theory]
    [MemberData(nameof(ValidPhones), MemberType = typeof(ValidationData))]
    public void Validate_PalestinianPhone_HasNoError(string phone)
    {
        _validator.TestValidate(Valid with { PhoneNumber = phone }).ShouldNotHaveValidationErrorFor(c => c.PhoneNumber);
    }

    [Theory]
    [MemberData(nameof(InvalidPhones), MemberType = typeof(ValidationData))]
    public void Validate_InvalidPhone_ReturnsFormatError(string phone)
    {
        _validator.TestValidate(Valid with { PhoneNumber = phone })
            .ShouldHaveValidationErrorFor(c => c.PhoneNumber)
            .WithErrorMessage("رقم الهاتف غير صالح.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingCity_HasNoError(string? city)
    {
        _validator.TestValidate(Valid with { City = city }).ShouldNotHaveValidationErrorFor(c => c.City);
    }

    [Fact]
    public void Validate_CityAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { City = Text(100) }).ShouldNotHaveValidationErrorFor(c => c.City);
    }

    [Fact]
    public void Validate_CityOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { City = Text(101) }).ShouldHaveValidationErrorFor(c => c.City);
    }
}
