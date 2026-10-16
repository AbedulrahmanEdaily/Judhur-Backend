using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Reports;

public sealed class ReportCreateTests
{
    [Fact]
    public void Create_ValidReport_ReturnsSuccess()
    {
        var result = ReportFactory.CreateResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ValidReport_CopiesData()
    {
        var reporterId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();

        var report = ReportFactory.Create(reporterId, propertyId, reason: ReportReason.Duplicate);

        Assert.Equal(reporterId, report.ReporterId);
        Assert.Equal(propertyId, report.PropertyId);
        Assert.Equal(ReportReason.Duplicate, report.Reason);
        Assert.Equal(ReportFactory.DefaultDetails, report.Details);
    }

    [Fact]
    public void Create_ValidReport_StartsPendingWithoutReviewData()
    {
        var report = ReportFactory.Create();

        Assert.Equal(ReportStatus.Pending, report.Status);
        Assert.Null(report.ReviewedBy);
        Assert.Null(report.ReviewedAtUtc);
        Assert.Null(report.AdminNote);
    }

    [Fact]
    public void Create_GivenId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var report = ReportFactory.Create(id: id);

        Assert.Equal(id, report.Id);
    }

    [Fact]
    public void Create_EmptyId_GeneratesNewId()
    {
        var report = ReportFactory.Create(id: Guid.Empty);

        Assert.NotEqual(Guid.Empty, report.Id);
    }

    [Fact]
    public void Create_EmptyReporter_ReturnsReporterIdRequired()
    {
        var result = ReportFactory.CreateResult(reporterId: Guid.Empty);

        Assert.Equal(ReportErrors.ReporterIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptyProperty_ReturnsPropertyIdRequired()
    {
        var result = ReportFactory.CreateResult(propertyId: Guid.Empty);

        Assert.Equal(ReportErrors.PropertyIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptyReporterAndProperty_ReturnsReporterIdRequiredFirst()
    {
        var result = ReportFactory.CreateResult(reporterId: Guid.Empty, propertyId: Guid.Empty);

        Assert.Equal(ReportErrors.ReporterIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ReporterIsSeller_ReturnsCannotReportOwnProperty()
    {
        var sellerId = Guid.NewGuid();

        var result = ReportFactory.CreateResult(reporterId: sellerId, sellerId: sellerId);

        Assert.Equal(ReportErrors.CannotReportOwnProperty.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ReporterIsSellerWithInvalidReason_ReturnsCannotReportOwnPropertyFirst()
    {
        var sellerId = Guid.NewGuid();

        var result = ReportFactory.CreateResult(reporterId: sellerId, sellerId: sellerId, reason: (ReportReason)42);

        Assert.Equal(ReportErrors.CannotReportOwnProperty.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(ReportReason.Fraud)]
    [InlineData(ReportReason.IncorrectInformation)]
    [InlineData(ReportReason.Duplicate)]
    [InlineData(ReportReason.InappropriateContent)]
    [InlineData(ReportReason.Other)]
    public void Create_DefinedReason_ReturnsSuccess(ReportReason reason)
    {
        var result = ReportFactory.CreateResult(reason: reason);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(42)]
    public void Create_UndefinedReason_ReturnsInvalidReason(int reason)
    {
        var result = ReportFactory.CreateResult(reason: (ReportReason)reason);

        Assert.Equal(ReportErrors.InvalidReason.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_OtherReasonWithoutDetails_ReturnsDetailsRequiredForOtherReason(string? details)
    {
        var result = ReportFactory.CreateResult(reason: ReportReason.Other, details: details);

        Assert.Equal(ReportErrors.DetailsRequiredForOtherReason.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_OtherReasonWithDetails_ReturnsSuccess()
    {
        var report = ReportFactory.Create(reason: ReportReason.Other, details: "صاحب الإعلان لا يرد");

        Assert.Equal("صاحب الإعلان لا يرد", report.Details);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void Create_NonOtherReasonWithoutDetails_StoresNullDetails(string? details)
    {
        var report = ReportFactory.Create(reason: ReportReason.Fraud, details: details);

        Assert.Null(report.Details);
    }

    [Fact]
    public void Create_DetailsWithSurroundingWhitespace_StoresTrimmedDetails()
    {
        var report = ReportFactory.Create(details: $"  {ReportFactory.DefaultDetails}\n");

        Assert.Equal(ReportFactory.DefaultDetails, report.Details);
    }

    [Fact]
    public void Create_DetailsAtLimit_ReturnsSuccess()
    {
        var result = ReportFactory.CreateResult(details: new string('ت', Report.MaxDetailsLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_DetailsAtLimitAfterTrimming_ReturnsSuccess()
    {
        var details = new string('ت', Report.MaxDetailsLength);

        var report = ReportFactory.Create(details: $"   {details}   ");

        Assert.Equal(details, report.Details);
    }

    [Fact]
    public void Create_DetailsOverLimit_ReturnsDetailsTooLong()
    {
        var result = ReportFactory.CreateResult(details: new string('ت', Report.MaxDetailsLength + 1));

        Assert.Equal(ReportErrors.DetailsTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_OtherReasonWithDetailsOverLimit_ReturnsDetailsTooLong()
    {
        var result = ReportFactory.CreateResult(reason: ReportReason.Other, details: new string('ت', Report.MaxDetailsLength + 1));

        Assert.Equal(ReportErrors.DetailsTooLong.Code, result.TopError.Code);
    }
}
