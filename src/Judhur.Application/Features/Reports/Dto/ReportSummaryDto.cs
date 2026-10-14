using Judhur.Domain.Reports.Enums;

namespace Judhur.Application.Features.Reports.Dto;

public sealed record ReportSummaryDto(
    Guid Id,
    Guid PropertyId,
    string PropertyTitle,
    bool PropertyIsDeleted,
    ReportReason Reason,
    ReportStatus Status,
    int PendingReportsOnProperty,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc);
