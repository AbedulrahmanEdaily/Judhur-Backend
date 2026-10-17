using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.ConfirmEmail;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class ConfirmEmailCommandValidatorTests
{
    private readonly ConfirmEmailCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new ConfirmEmailCommand(Guid.NewGuid(), "token")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyUserId_ReturnsError()
    {
        _validator.TestValidate(new ConfirmEmailCommand(Guid.Empty, "token"))
            .ShouldHaveValidationErrorFor(c => c.UserId)
            .WithErrorMessage("معرّف المستخدم مطلوب.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_BlankToken_ReturnsError(string token)
    {
        _validator.TestValidate(new ConfirmEmailCommand(Guid.NewGuid(), token))
            .ShouldHaveValidationErrorFor(c => c.Token)
            .WithErrorMessage("رمز التأكيد مطلوب.");
    }
}
