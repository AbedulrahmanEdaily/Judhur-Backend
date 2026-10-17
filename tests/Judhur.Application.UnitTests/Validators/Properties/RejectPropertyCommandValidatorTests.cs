using FluentValidation.TestHelper;

using Judhur.Application.Features.Properties.Commands.RejectProperty;
using Judhur.Domain.Properties;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Properties;

public sealed class RejectPropertyCommandValidatorTests
{
    private static readonly RejectPropertyCommand Valid = new(Guid.NewGuid(), "الصور غير واضحة", ValidRowVersion);

    private readonly RejectPropertyCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyPropertyId_ReturnsError()
    {
        _validator.TestValidate(Valid with { PropertyId = Guid.Empty }).ShouldHaveValidationErrorFor(c => c.PropertyId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankReason_ReturnsError(string reason)
    {
        _validator.TestValidate(Valid with { RejectionReason = reason })
            .ShouldHaveValidationErrorFor(c => c.RejectionReason)
            .WithErrorMessage("سبب الرفض مطلوب.");
    }

    [Fact]
    public void Validate_ReasonAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { RejectionReason = Text(Property.MaxRejectionReasonLength) })
            .ShouldNotHaveValidationErrorFor(c => c.RejectionReason);
    }

    [Fact]
    public void Validate_ReasonOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { RejectionReason = Text(Property.MaxRejectionReasonLength + 1) })
            .ShouldHaveValidationErrorFor(c => c.RejectionReason);
    }

    [Theory]
    [MemberData(nameof(InvalidRowVersions), MemberType = typeof(ValidationData))]
    public void Validate_InvalidRowVersion_ReturnsError(string? rowVersion)
    {
        _validator.TestValidate(Valid with { RowVersion = rowVersion! })
            .ShouldHaveValidationErrorFor(c => c.RowVersion)
            .WithErrorMessage("نسخة العقار غير صالحة، حدّث صفحة المراجعة.");
    }
}
