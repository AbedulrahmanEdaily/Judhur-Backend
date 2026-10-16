using FluentValidation.TestHelper;

using Judhur.Application.Features.Properties.Commands.ApproveProperty;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Properties;

public sealed class ApprovePropertyCommandValidatorTests
{
    private readonly ApprovePropertyCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new ApprovePropertyCommand(Guid.NewGuid(), ValidRowVersion)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyPropertyId_ReturnsError()
    {
        _validator.TestValidate(new ApprovePropertyCommand(Guid.Empty, ValidRowVersion))
            .ShouldHaveValidationErrorFor(c => c.PropertyId)
            .WithErrorMessage("معرّف العقار مطلوب.");
    }

    [Theory]
    [MemberData(nameof(InvalidRowVersions), MemberType = typeof(ValidationData))]
    public void Validate_InvalidRowVersion_ReturnsError(string? rowVersion)
    {
        _validator.TestValidate(new ApprovePropertyCommand(Guid.NewGuid(), rowVersion!))
            .ShouldHaveValidationErrorFor(c => c.RowVersion)
            .WithErrorMessage("نسخة العقار غير صالحة، حدّث صفحة المراجعة.");
    }
}
