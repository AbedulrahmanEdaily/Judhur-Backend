using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.Login;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new LoginCommand(ValidEmail, "any")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyEmail_ReturnsOnlyRequiredError()
    {
        var result = _validator.TestValidate(new LoginCommand("", "any"));

        result.ShouldHaveValidationErrorFor(c => c.Email).WithErrorMessage("البريد الإلكتروني مطلوب.");
        Assert.Single(result.Errors, e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Theory]
    [MemberData(nameof(InvalidEmails), MemberType = typeof(ValidationData))]
    public void Validate_MalformedEmail_ReturnsFormatError(string email)
    {
        _validator.TestValidate(new LoginCommand(email, "any"))
            .ShouldHaveValidationErrorFor(c => c.Email)
            .WithErrorMessage("صيغة البريد الإلكتروني غير صحيحة.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_BlankPassword_ReturnsError(string password)
    {
        _validator.TestValidate(new LoginCommand(ValidEmail, password))
            .ShouldHaveValidationErrorFor(c => c.Password)
            .WithErrorMessage("كلمة المرور مطلوبة.");
    }

    [Fact]
    public void Validate_WeakPassword_HasNoError()
    {
        _validator.TestValidate(new LoginCommand(ValidEmail, "x")).ShouldNotHaveValidationErrorFor(c => c.Password);
    }
}
