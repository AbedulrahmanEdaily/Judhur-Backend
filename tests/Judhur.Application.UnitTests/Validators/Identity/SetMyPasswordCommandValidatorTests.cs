using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.SetMyPassword;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class SetMyPasswordCommandValidatorTests
{
    private static readonly SetMyPasswordCommand Valid = new("OldPassw0rd", ValidPassword);

    private readonly SetMyPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NoCurrentPassword_HasNoErrors()
    {
        _validator.TestValidate(Valid with { CurrentPassword = null }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyNewPassword_ReturnsError()
    {
        _validator.TestValidate(Valid with { NewPassword = "" })
            .ShouldHaveValidationErrorFor(c => c.NewPassword)
            .WithErrorMessage("كلمة المرور الجديدة مطلوبة.");
    }

    [Theory]
    [MemberData(nameof(WeakPasswords), MemberType = typeof(ValidationData))]
    public void Validate_WeakNewPassword_ReturnsError(string password)
    {
        _validator.TestValidate(Valid with { NewPassword = password }).ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Theory]
    [MemberData(nameof(StrongPasswords), MemberType = typeof(ValidationData))]
    public void Validate_StrongNewPassword_HasNoError(string password)
    {
        _validator.TestValidate(Valid with { NewPassword = password }).ShouldNotHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void Validate_NewPasswordSameAsCurrent_ReturnsError()
    {
        _validator.TestValidate(Valid with { CurrentPassword = ValidPassword, NewPassword = ValidPassword })
            .ShouldHaveValidationErrorFor(c => c.NewPassword)
            .WithErrorMessage("يجب أن تختلف كلمة المرور الجديدة عن الحالية.");
    }

    [Fact]
    public void Validate_NewPasswordDiffersOnlyByCase_HasNoError()
    {
        _validator.TestValidate(Valid with { CurrentPassword = "passW0RD", NewPassword = "Passw0rd" })
            .ShouldNotHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void Validate_GoogleIdTokenIsNotValidated()
    {
        _validator.TestValidate(Valid with { GoogleIdToken = "" }).ShouldNotHaveAnyValidationErrors();
    }
}
