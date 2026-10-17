using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.Logout;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new LogoutCommand("refresh")).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_BlankRefreshToken_ReturnsError(string token)
    {
        _validator.TestValidate(new LogoutCommand(token))
            .ShouldHaveValidationErrorFor(c => c.RefreshToken)
            .WithErrorMessage("رمز التحديث مطلوب.");
    }
}
