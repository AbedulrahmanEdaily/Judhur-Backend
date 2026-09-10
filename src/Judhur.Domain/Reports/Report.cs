using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Reports.Enums;

namespace Judhur.Domain.Reports;

public sealed class Report : AuditableEntity
{
    public const int MaxDetailsLength = 1000;
    public const int MaxAdminNoteLength = 1000;

    public Guid ReporterId { get; private set; }
    public Guid PropertyId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string? Details { get; private set; }
    public ReportStatus Status { get; private set; } = ReportStatus.Pending;
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? AdminNote { get; private set; }

    private Report() { }

    private Report(Guid id, Guid reporterId, Guid propertyId, ReportReason reason, string? details) : base(id)
    {
        ReporterId = reporterId;
        PropertyId = propertyId;
        Reason = reason;
        Details = details;
    }

    public static Result<Report> Create(Guid id, Guid reporterId, Guid propertyId, ReportReason reason, string? details)
    {
        if (reporterId == Guid.Empty)
        {
            return ReportErrors.ReporterIdRequired;
        }
        if (propertyId == Guid.Empty)
        {
            return ReportErrors.PropertyIdRequired;
        }
        if (!Enum.IsDefined(reason))
        {
            return ReportErrors.InvalidReason;
        }
        if (reason == ReportReason.Other && string.IsNullOrWhiteSpace(details))
        {
            return ReportErrors.DetailsRequiredForOtherReason;
        }

        if (details?.Length > MaxDetailsLength)
        {
            return ReportErrors.DetailsTooLong;
        }
        return new Report(id, reporterId, propertyId, reason, details);
    }

    public Result<Updated> Resolve(Guid adminId, DateTimeOffset reviewedAtUtc, string? adminNote)
        => MarkAsReviewed(ReportStatus.Resolved, adminId, reviewedAtUtc, adminNote);

    public Result<Updated> Dismiss(Guid adminId, DateTimeOffset reviewedAtUtc, string? adminNote)
        => MarkAsReviewed(ReportStatus.Dismissed, adminId, reviewedAtUtc, adminNote);

    private Result<Updated> MarkAsReviewed(ReportStatus status, Guid adminId, DateTimeOffset reviewedAtUtc, string? adminNote)
    {
        if (adminId == Guid.Empty)
        {
            return ReportErrors.AdminIdRequired;
        }
        if (Status != ReportStatus.Pending)
        {
            return ReportErrors.AlreadyReviewed;
        }

        if (adminNote?.Length > MaxAdminNoteLength)
        {
            return ReportErrors.AdminNoteTooLong;
        }
        Status = status;
        ReviewedBy = adminId;
        ReviewedAtUtc = reviewedAtUtc;
        AdminNote = adminNote;
        return Result.Updated;
    }
}
