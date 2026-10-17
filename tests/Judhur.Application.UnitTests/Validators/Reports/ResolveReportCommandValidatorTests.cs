using FluentValidation.TestHelper;

using Judhur.Application.Features.Reports.Commands.ResolveReport;
using Judhur.Domain.Properties;
using Judhur.Domain.Reports;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Reports;

public sealed class ResolveReportCommandValidatorTests
{
    private static readonly ResolveReportCommand Valid = new(Guid.NewGuid(), null, false, null);

    private readonly ResolveReportCommandValidator _validator = new();

    [Fact]
    public void Validate_ResolveWithoutSuspension_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ResolveWithSuspensionAndReason_HasNoErrors()
    {
        _validator.TestValidate(Valid with { SuspendProperty = true, SuspensionReason = "إعلان مضلل" }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyReportId_ReturnsError()
    {
        _validator.TestValidate(Valid with { ReportId = Guid.Empty }).ShouldHaveValidationErrorFor(c => c.ReportId);
    }

    [Fact]
    public void Validate_NoteOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { AdminNote = Text(Report.MaxAdminNoteLength + 1) }).ShouldHaveValidationErrorFor(c => c.AdminNote);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_SuspendWithoutReason_ReturnsError(string? reason)
    {
        _validator.TestValidate(Valid with { SuspendProperty = true, SuspensionReason = reason })
            .ShouldHaveValidationErrorFor(c => c.SuspensionReason)
            .WithErrorMessage("سبب إيقاف العقار مطلوب.");
    }

    [Fact]
    public void Validate_SuspendWithReasonAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { SuspendProperty = true, SuspensionReason = Text(Property.MaxRejectionReasonLength) })
            .ShouldNotHaveValidationErrorFor(c => c.SuspensionReason);
    }

    [Fact]
    public void Validate_SuspendWithReasonOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { SuspendProperty = true, SuspensionReason = Text(Property.MaxRejectionReasonLength + 1) })
            .ShouldHaveValidationErrorFor(c => c.SuspensionReason);
    }

    [Fact]
    public void Validate_NoSuspensionWithLongReason_HasNoError()
    {
        _validator.TestValidate(Valid with { SuspensionReason = Text(Property.MaxRejectionReasonLength + 1) })
            .ShouldNotHaveValidationErrorFor(c => c.SuspensionReason);
    }
}
