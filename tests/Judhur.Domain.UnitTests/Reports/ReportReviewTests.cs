using Judhur.Domain.Common.Results;
using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Reports;

public sealed class ReportReviewTests
{
    private static readonly Guid AdminId = ReportFactory.DefaultAdminId;
    private static readonly DateTimeOffset ReviewedAt = TestClock.UtcNow.AddHours(6);

    private static Result<Updated> Review(Report report, ReportStatus outcome, Guid adminId, string? adminNote)
        => outcome == ReportStatus.Resolved
            ? report.Resolve(adminId, ReviewedAt, adminNote)
            : report.Dismiss(adminId, ReviewedAt, adminNote);

    [Fact]
    public void Resolve_PendingReport_SetsResolvedWithReviewData()
    {
        var report = ReportFactory.Create();

        var result = report.Resolve(AdminId, ReviewedAt, ReportFactory.DefaultAdminNote);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.Equal(AdminId, report.ReviewedBy);
        Assert.Equal(ReviewedAt, report.ReviewedAtUtc);
        Assert.Equal(ReportFactory.DefaultAdminNote, report.AdminNote);
    }

    [Fact]
    public void Dismiss_PendingReport_SetsDismissedWithReviewData()
    {
        var report = ReportFactory.Create();

        var result = report.Dismiss(AdminId, ReviewedAt, ReportFactory.DefaultAdminNote);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Equal(AdminId, report.ReviewedBy);
        Assert.Equal(ReviewedAt, report.ReviewedAtUtc);
        Assert.Equal(ReportFactory.DefaultAdminNote, report.AdminNote);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_PendingReport_KeepsReportData(ReportStatus outcome)
    {
        var report = ReportFactory.Create();

        Review(report, outcome, AdminId, null);

        Assert.Equal(ReportFactory.DefaultReporterId, report.ReporterId);
        Assert.Equal(ReportFactory.DefaultPropertyId, report.PropertyId);
        Assert.Equal(ReportReason.Fraud, report.Reason);
        Assert.Equal(ReportFactory.DefaultDetails, report.Details);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved, null)]
    [InlineData(ReportStatus.Resolved, "")]
    [InlineData(ReportStatus.Dismissed, "   ")]
    [InlineData(ReportStatus.Dismissed, null)]
    public void ResolveOrDismiss_BlankNote_StoresNullNote(ReportStatus outcome, string? adminNote)
    {
        var report = ReportFactory.Create();

        var result = Review(report, outcome, AdminId, adminNote);

        Assert.True(result.IsSuccess);
        Assert.Null(report.AdminNote);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_NoteWithSurroundingWhitespace_StoresTrimmedNote(ReportStatus outcome)
    {
        var report = ReportFactory.Create();

        Review(report, outcome, AdminId, $"\n {ReportFactory.DefaultAdminNote}  ");

        Assert.Equal(ReportFactory.DefaultAdminNote, report.AdminNote);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_NoteAtLimit_ReturnsSuccess(ReportStatus outcome)
    {
        var report = ReportFactory.Create();

        var result = Review(report, outcome, AdminId, new string('م', Report.MaxAdminNoteLength));

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_NoteAtLimitAfterTrimming_ReturnsSuccess(ReportStatus outcome)
    {
        var report = ReportFactory.Create();

        var result = Review(report, outcome, AdminId, $"  {new string('م', Report.MaxAdminNoteLength)}  ");

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_NoteOverLimit_ReturnsAdminNoteTooLong(ReportStatus outcome)
    {
        var report = ReportFactory.Create();

        var result = Review(report, outcome, AdminId, new string('م', Report.MaxAdminNoteLength + 1));

        Assert.Equal(ReportErrors.AdminNoteTooLong.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_NoteOverLimit_StaysPendingWithoutReviewData(ReportStatus outcome)
    {
        var report = ReportFactory.Create();

        Review(report, outcome, AdminId, new string('م', Report.MaxAdminNoteLength + 1));

        Assert.Equal(ReportStatus.Pending, report.Status);
        Assert.Null(report.ReviewedBy);
        Assert.Null(report.ReviewedAtUtc);
        Assert.Null(report.AdminNote);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_EmptyAdmin_ReturnsAdminIdRequired(ReportStatus outcome)
    {
        var report = ReportFactory.Create();

        var result = Review(report, outcome, Guid.Empty, ReportFactory.DefaultAdminNote);

        Assert.Equal(ReportErrors.AdminIdRequired.Code, result.TopError.Code);
        Assert.Equal(ReportStatus.Pending, report.Status);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_EmptyAdminOnReviewedReport_ReturnsAdminIdRequiredFirst(ReportStatus outcome)
    {
        var report = ReportFactory.CreateResolved();

        var result = Review(report, outcome, Guid.Empty, null);

        Assert.Equal(ReportErrors.AdminIdRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_ResolvedReport_ReturnsAlreadyReviewed(ReportStatus outcome)
    {
        var report = ReportFactory.CreateResolved();

        var result = Review(report, outcome, Guid.NewGuid(), "ملاحظة أخرى");

        Assert.Equal(ReportErrors.AlreadyReviewed.Code, result.TopError.Code);
        Assert.Equal(ReportStatus.Resolved, report.Status);
    }

    [Theory]
    [InlineData(ReportStatus.Resolved)]
    [InlineData(ReportStatus.Dismissed)]
    public void ResolveOrDismiss_DismissedReport_ReturnsAlreadyReviewed(ReportStatus outcome)
    {
        var report = ReportFactory.CreateDismissed();

        var result = Review(report, outcome, Guid.NewGuid(), "ملاحظة أخرى");

        Assert.Equal(ReportErrors.AlreadyReviewed.Code, result.TopError.Code);
        Assert.Equal(ReportStatus.Dismissed, report.Status);
    }

    [Fact]
    public void Resolve_AlreadyReviewed_KeepsOriginalReviewData()
    {
        var report = ReportFactory.CreateDismissed();

        report.Resolve(Guid.NewGuid(), ReviewedAt, "ملاحظة أخرى");

        Assert.Equal(ReportFactory.DefaultAdminId, report.ReviewedBy);
        Assert.Equal(TestClock.UtcNow, report.ReviewedAtUtc);
        Assert.Equal(ReportFactory.DefaultAdminNote, report.AdminNote);
    }

    [Fact]
    public void Resolve_ReviewedReportWithNoteOverLimit_ReturnsAlreadyReviewedFirst()
    {
        var report = ReportFactory.CreateResolved();

        var result = report.Resolve(AdminId, ReviewedAt, new string('م', Report.MaxAdminNoteLength + 1));

        Assert.Equal(ReportErrors.AlreadyReviewed.Code, result.TopError.Code);
    }
}
