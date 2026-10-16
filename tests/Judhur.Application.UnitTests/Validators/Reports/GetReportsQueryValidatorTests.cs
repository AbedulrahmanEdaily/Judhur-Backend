using FluentValidation.TestHelper;

using Judhur.Application.Features.Reports.Queries.GetReports;
using Judhur.Domain.Reports.Enums;

namespace Judhur.Application.UnitTests.Validators.Reports;

public sealed class GetReportsQueryValidatorTests
{
    private readonly GetReportsQueryValidator _validator = new();

    [Theory]
    [InlineData(ReportStatus.Pending)]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void Validate_DefinedStatus_HasNoErrors(ReportStatus status)
    {
        _validator.TestValidate(new GetReportsQuery(status, 1, 20)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_UndefinedStatus_ReturnsError()
    {
        _validator.TestValidate(new GetReportsQuery((ReportStatus)99, 1, 20))
            .ShouldHaveValidationErrorFor(q => q.Status)
            .WithErrorMessage("حالة البلاغ غير صالحة.");
    }
}
