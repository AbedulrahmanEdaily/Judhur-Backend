using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.RequestEmailChange;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class RequestEmailChangeCommandValidatorTests
{
    private readonly RequestEmailChangeCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new RequestEmailChangeCommand(ValidEmail, "Passw0rd")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NoPasswordOrGoogleToken_HasNoErrors()
    {
        _validator.TestValidate(new RequestEmailChangeCommand(ValidEmail, null)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyNewEmail_ReturnsError()
    {
        _validator.TestValidate(new RequestEmailChangeCommand("", null))
            .ShouldHaveValidationErrorFor(c => c.NewEmail)
            .WithErrorMessage("البريد الإلكتروني الجديد مطلوب.");
    }

    [Theory]
    [MemberData(nameof(InvalidEmails), MemberType = typeof(ValidationData))]
    public void Validate_MalformedNewEmail_ReturnsError(string email)
    {
        _validator.TestValidate(new RequestEmailChangeCommand(email, null))
            .ShouldHaveValidationErrorFor(c => c.NewEmail)
            .WithErrorMessage("صيغة البريد الإلكتروني غير صحيحة.");
    }

    [Fact]
    public void Validate_NewEmailAtMaxLength_HasNoError()
    {
        _validator.TestValidate(new RequestEmailChangeCommand(EmailOfLength(256), null)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NewEmailOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(new RequestEmailChangeCommand(EmailOfLength(257), null)).ShouldHaveValidationErrorFor(c => c.NewEmail);
    }
}
