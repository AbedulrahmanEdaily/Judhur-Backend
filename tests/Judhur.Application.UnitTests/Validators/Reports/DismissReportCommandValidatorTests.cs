using FluentValidation.TestHelper;

using Judhur.Application.Features.Reports.Commands.DismissReport;
using Judhur.Domain.Reports;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Reports;

public sealed class DismissReportCommandValidatorTests
{
    private readonly DismissReportCommandValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("لا مخالفة")]
    public void Validate_ValidCommand_HasNoErrors(string? note)
    {
        _validator.TestValidate(new DismissReportCommand(Guid.NewGuid(), note)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyReportId_ReturnsError()
    {
        _validator.TestValidate(new DismissReportCommand(Guid.Empty, null))
            .ShouldHaveValidationErrorFor(c => c.ReportId)
            .WithErrorMessage("معرّف البلاغ مطلوب.");
    }

    [Fact]
    public void Validate_NoteAtMaxLength_HasNoError()
    {
        _validator.TestValidate(new DismissReportCommand(Guid.NewGuid(), Text(Report.MaxAdminNoteLength)))
            .ShouldNotHaveValidationErrorFor(c => c.AdminNote);
    }

    [Fact]
    public void Validate_NoteOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(new DismissReportCommand(Guid.NewGuid(), Text(Report.MaxAdminNoteLength + 1)))
            .ShouldHaveValidationErrorFor(c => c.AdminNote);
    }
}
