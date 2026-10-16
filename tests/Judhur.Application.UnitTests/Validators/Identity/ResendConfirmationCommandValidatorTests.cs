using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.ResendConfirmation;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class ResendConfirmationCommandValidatorTests
{
    private readonly ResendConfirmationCommandValidator _validator = new();

    [Theory]
    [MemberData(nameof(ValidEmails), MemberType = typeof(ValidationData))]
    public void Validate_WellFormedEmail_HasNoErrors(string email)
    {
        _validator.TestValidate(new ResendConfirmationCommand(email)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyEmail_ReturnsOnlyRequiredError()
    {
        var result = _validator.TestValidate(new ResendConfirmationCommand(""));

        result.ShouldHaveValidationErrorFor(c => c.Email).WithErrorMessage("البريد الإلكتروني مطلوب.");
        Assert.Single(result.Errors);
    }

    [Theory]
    [MemberData(nameof(InvalidEmails), MemberType = typeof(ValidationData))]
    public void Validate_MalformedEmail_ReturnsFormatError(string email)
    {
        _validator.TestValidate(new ResendConfirmationCommand(email))
            .ShouldHaveValidationErrorFor(c => c.Email)
            .WithErrorMessage("صيغة البريد الإلكتروني غير صحيحة.");
    }
}
