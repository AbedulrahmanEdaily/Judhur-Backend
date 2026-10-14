using Judhur.Domain.Reports.Enums;

namespace Judhur.Application.Features.Reports.Dto;

public sealed record MyReportDto(
    Guid Id,
    Guid PropertyId,
    string PropertyTitle,
    ReportReason Reason,
    string? Details,
    ReportStatus Status,
    string? AdminNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc);
