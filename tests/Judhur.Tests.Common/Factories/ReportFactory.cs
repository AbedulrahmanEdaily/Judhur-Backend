using Judhur.Domain.Common.Results;
using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;

namespace Judhur.Tests.Common.Factories;

public static class ReportFactory
{
    public const string DefaultDetails = "السعر المعروض غير حقيقي";
    public const string DefaultAdminNote = "تمت مراجعة البلاغ";

    public static readonly Guid DefaultReporterId = Guid.Parse("0199a1b2-0000-7000-8000-000000000003");
    public static readonly Guid DefaultPropertyId = Guid.Parse("0199a1b2-0000-7000-8000-0000000000b2");
    public static readonly Guid DefaultSellerId = PropertyFactory.DefaultSellerId;
    public static readonly Guid DefaultAdminId = Guid.Parse("0199a1b2-0000-7000-8000-0000000000a2");

    public static Result<Report> CreateResult(
        Guid? reporterId = null,
        Guid? propertyId = null,
        Guid? sellerId = null,
        ReportReason reason = ReportReason.Fraud,
        string? details = DefaultDetails,
        Guid? id = null)
        => Report.Create(
            id ?? Guid.NewGuid(),
            reporterId ?? DefaultReporterId,
            propertyId ?? DefaultPropertyId,
            sellerId ?? DefaultSellerId,
            reason,
            details);

    public static Report Create(
        Guid? reporterId = null,
        Guid? propertyId = null,
        Guid? sellerId = null,
        ReportReason reason = ReportReason.Fraud,
        string? details = DefaultDetails,
        Guid? id = null)
        => CreateResult(reporterId, propertyId, sellerId, reason, details, id).GetValueOrThrow();

    public static Report CreateResolved()
    {
        var report = Create();
        report.Resolve(DefaultAdminId, TestClock.UtcNow, DefaultAdminNote).ThrowIfError();
        return report;
    }

    public static Report CreateDismissed()
    {
        var report = Create();
        report.Dismiss(DefaultAdminId, TestClock.UtcNow, DefaultAdminNote).ThrowIfError();
        return report;
    }
}
