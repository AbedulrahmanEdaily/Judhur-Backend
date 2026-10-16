using FluentValidation.TestHelper;

using Judhur.Application.Features.Users.Commands.BanUser;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Users;

public sealed class BanUserCommandValidatorTests
{
    private readonly BanUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new BanUserCommand(Guid.NewGuid(), "احتيال متكرر")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyUserId_ReturnsError()
    {
        _validator.TestValidate(new BanUserCommand(Guid.Empty, "احتيال"))
            .ShouldHaveValidationErrorFor(c => c.UserId)
            .WithErrorMessage("معرّف المستخدم مطلوب.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankReason_ReturnsError(string reason)
    {
        _validator.TestValidate(new BanUserCommand(Guid.NewGuid(), reason))
            .ShouldHaveValidationErrorFor(c => c.Reason)
            .WithErrorMessage("سبب الحظر مطلوب.");
    }

    [Fact]
    public void Validate_ReasonAtMaxLengthAfterTrim_HasNoError()
    {
        _validator.TestValidate(new BanUserCommand(Guid.NewGuid(), $"  {Text(BanUserCommandValidator.MaxReasonLength)}  "))
            .ShouldNotHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Validate_ReasonOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(new BanUserCommand(Guid.NewGuid(), Text(BanUserCommandValidator.MaxReasonLength + 1)))
            .ShouldHaveValidationErrorFor(c => c.Reason)
            .WithErrorMessage($"لا يمكن أن يتجاوز سبب الحظر {BanUserCommandValidator.MaxReasonLength} حرف.");
    }
}
