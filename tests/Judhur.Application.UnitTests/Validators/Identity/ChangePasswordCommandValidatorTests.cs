using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.ChangePassword;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class ChangePasswordCommandValidatorTests
{
    private static readonly ChangePasswordCommand Valid = new("123456", ValidPassword, ValidEmail);

    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCode_ReturnsError()
    {
        _validator.TestValidate(Valid with { Code = "" })
            .ShouldHaveValidationErrorFor(c => c.Code)
            .WithErrorMessage("رمز استعادة كلمة المرور مطلوب.");
    }

    [Fact]
    public void Validate_EmptyEmail_ReturnsError()
    {
        _validator.TestValidate(Valid with { Email = "" }).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [MemberData(nameof(InvalidEmails), MemberType = typeof(ValidationData))]
    public void Validate_MalformedEmail_ReturnsError(string email)
    {
        _validator.TestValidate(Valid with { Email = email })
            .ShouldHaveValidationErrorFor(c => c.Email)
            .WithErrorMessage("صيغة البريد الإلكتروني غير صحيحة.");
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
}
