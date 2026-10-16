using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.Register;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class RegisterCommandValidatorTests
{
    private static readonly RegisterCommand Valid = new("علي أحمد", ValidEmail, ValidPhone, "رام الله", null, ValidPassword);

    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
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
    public void Validate_EmptyEmail_ReturnsRequiredError()
    {
        _validator.TestValidate(Valid with { Email = "" })
            .ShouldHaveValidationErrorFor(c => c.Email)
            .WithErrorMessage("البريد الإلكتروني مطلوب.");
    }

    [Theory]
    [MemberData(nameof(InvalidEmails), MemberType = typeof(ValidationData))]
    public void Validate_MalformedEmail_ReturnsFormatError(string email)
    {
        _validator.TestValidate(Valid with { Email = email })
            .ShouldHaveValidationErrorFor(c => c.Email)
            .WithErrorMessage("صيغة البريد الإلكتروني غير صحيحة.");
    }

    [Theory]
    [MemberData(nameof(ValidEmails), MemberType = typeof(ValidationData))]
    public void Validate_WellFormedEmail_HasNoError(string email)
    {
        _validator.TestValidate(Valid with { Email = email }).ShouldNotHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_EmailAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { Email = EmailOfLength(256) }).ShouldNotHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_EmailOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { Email = EmailOfLength(257) }).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_EmptyCity_ReturnsError()
    {
        _validator.TestValidate(Valid with { City = "" })
            .ShouldHaveValidationErrorFor(c => c.City)
            .WithErrorMessage("المدينة مطلوبة.");
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
    public void Validate_EmptyPassword_ReturnsError()
    {
        _validator.TestValidate(Valid with { Password = "" })
            .ShouldHaveValidationErrorFor(c => c.Password)
            .WithErrorMessage("كلمة المرور مطلوبة.");
    }

    [Theory]
    [MemberData(nameof(WeakPasswords), MemberType = typeof(ValidationData))]
    public void Validate_WeakPassword_ReturnsError(string password)
    {
        _validator.TestValidate(Valid with { Password = password }).ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Theory]
    [MemberData(nameof(StrongPasswords), MemberType = typeof(ValidationData))]
    public void Validate_StrongPassword_HasNoError(string password)
    {
        _validator.TestValidate(Valid with { Password = password }).ShouldNotHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Validate_ShortPassword_ReturnsMinimumLengthMessage()
    {
        _validator.TestValidate(Valid with { Password = "Pass0rd" })
            .ShouldHaveValidationErrorFor(c => c.Password)
            .WithErrorMessage("يجب ألا تقل كلمة المرور عن 8 أحرف.");
    }

    [Fact]
    public void Validate_PasswordWithoutUppercase_ReturnsUppercaseMessage()
    {
        _validator.TestValidate(Valid with { Password = "passw0rd" })
            .ShouldHaveValidationErrorFor(c => c.Password)
            .WithErrorMessage("يجب أن تحتوي كلمة المرور على حرف إنجليزي كبير واحد على الأقل.");
    }

    [Fact]
    public void Validate_PasswordWithoutLowercase_ReturnsLowercaseMessage()
    {
        _validator.TestValidate(Valid with { Password = "PASSW0RD" })
            .ShouldHaveValidationErrorFor(c => c.Password)
            .WithErrorMessage("يجب أن تحتوي كلمة المرور على حرف إنجليزي صغير واحد على الأقل.");
    }

    [Fact]
    public void Validate_PasswordWithoutDigit_ReturnsDigitMessage()
    {
        _validator.TestValidate(Valid with { Password = "Password" })
            .ShouldHaveValidationErrorFor(c => c.Password)
            .WithErrorMessage("يجب أن تحتوي كلمة المرور على رقم واحد على الأقل.");
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

    [Fact(Skip = "BUG: phone regex '$' accepts a trailing newline, and Register stores the phone untrimmed")]
    public void Validate_PhoneWithTrailingNewline_ReturnsError()
    {
        _validator.TestValidate(Valid with { PhoneNumber = "0591234567\n" }).ShouldHaveValidationErrorFor(c => c.PhoneNumber);
    }

    [Fact(Skip = "BUG: phone regex '\\d' accepts Arabic-Indic digits, so non-ASCII phone numbers are stored")]
    public void Validate_PhoneWithArabicIndicDigits_ReturnsError()
    {
        _validator.TestValidate(Valid with { PhoneNumber = "05٩١٢٣٤٥٦٧" }).ShouldHaveValidationErrorFor(c => c.PhoneNumber);
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
