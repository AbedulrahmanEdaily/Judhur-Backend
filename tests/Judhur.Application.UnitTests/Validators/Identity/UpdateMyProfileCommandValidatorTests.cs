using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.UpdateMyProfile;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class UpdateMyProfileCommandValidatorTests
{
    private static readonly UpdateMyProfileCommand Valid = new("علي أحمد", ValidPhone, "نابلس", "بائع عقارات");

    private readonly UpdateMyProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_BlankFullName_ReturnsError(string fullName)
    {
        _validator.TestValidate(Valid with { FullName = fullName })
            .ShouldHaveValidationErrorFor(c => c.FullName)
            .WithErrorMessage("الاسم الكامل مطلوب.");
    }

    [Fact]
    public void Validate_FullNameAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { FullName = Text(150) }).ShouldNotHaveValidationErrorFor(c => c.FullName);
    }

    [Fact]
    public void Validate_FullNameOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { FullName = Text(151) }).ShouldHaveValidationErrorFor(c => c.FullName);
    }

    [Fact]
    public void Validate_EmptyPhone_ReturnsRequiredError()
    {
        _validator.TestValidate(Valid with { PhoneNumber = "" })
            .ShouldHaveValidationErrorFor(c => c.PhoneNumber)
            .WithErrorMessage("رقم الهاتف مطلوب.");
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

    [Fact]
    public void Validate_EmptyCity_ReturnsError()
    {
        _validator.TestValidate(Valid with { City = "" }).ShouldHaveValidationErrorFor(c => c.City);
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

    [Fact]
    public void Validate_NullBio_HasNoError()
    {
        _validator.TestValidate(Valid with { Bio = null }).ShouldNotHaveValidationErrorFor(c => c.Bio);
    }

    [Fact]
    public void Validate_BioAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { Bio = Text(1000) }).ShouldNotHaveValidationErrorFor(c => c.Bio);
    }

    [Fact]
    public void Validate_BioOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { Bio = Text(1001) }).ShouldHaveValidationErrorFor(c => c.Bio);
    }
}
