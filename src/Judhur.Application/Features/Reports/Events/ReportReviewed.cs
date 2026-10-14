using Judhur.Domain.Reports.Enums;

using MediatR;

namespace Judhur.Application.Features.Reports.Events;

public sealed record ReportReviewed(
    Guid ReportId,
    Guid ReporterId,
    Guid PropertyId,
    string PropertyTitle,
    ReportStatus Outcome,
    string? AdminNote) : INotification;
