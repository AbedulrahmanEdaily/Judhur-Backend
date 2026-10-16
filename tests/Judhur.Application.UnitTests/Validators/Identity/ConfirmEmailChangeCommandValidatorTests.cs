using FluentValidation.TestHelper;

using Judhur.Application.Features.Identity.Commands.ConfirmEmailChange;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Identity;

public sealed class ConfirmEmailChangeCommandValidatorTests
{
    private static readonly ConfirmEmailChangeCommand Valid = new(Guid.NewGuid(), ValidEmail, "token");

    private readonly ConfirmEmailChangeCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyUserId_ReturnsError()
    {
        _validator.TestValidate(Valid with { UserId = Guid.Empty }).ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public void Validate_EmptyNewEmail_ReturnsError()
    {
        _validator.TestValidate(Valid with { NewEmail = "" })
            .ShouldHaveValidationErrorFor(c => c.NewEmail)
            .WithErrorMessage("البريد الإلكتروني الجديد مطلوب.");
    }

    [Theory]
    [MemberData(nameof(InvalidEmails), MemberType = typeof(ValidationData))]
    public void Validate_MalformedNewEmail_ReturnsError(string email)
    {
        _validator.TestValidate(Valid with { NewEmail = email }).ShouldHaveValidationErrorFor(c => c.NewEmail);
    }

    [Fact]
    public void Validate_EmptyToken_ReturnsError()
    {
        _validator.TestValidate(Valid with { Token = "" }).ShouldHaveValidationErrorFor(c => c.Token);
    }
}
