using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.RefreshToken;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new RefreshTokenCommand("refresh", "access")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyRefreshToken_ReturnsError()
    {
        _validator.TestValidate(new RefreshTokenCommand("", "access"))
            .ShouldHaveValidationErrorFor(c => c.RefreshToken)
            .WithErrorMessage("رمز التحديث مطلوب.");
    }

    [Fact]
    public void Validate_EmptyAccessToken_ReturnsError()
    {
        _validator.TestValidate(new RefreshTokenCommand("refresh", ""))
            .ShouldHaveValidationErrorFor(c => c.ExpiredAccessToken)
            .WithErrorMessage("رمز الوصول مطلوب.");
    }
}
