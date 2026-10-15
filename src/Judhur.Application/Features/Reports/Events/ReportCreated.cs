using Judhur.Domain.Reports.Enums;

using MediatR;

namespace Judhur.Application.Features.Reports.Events;

public sealed record ReportCreated(Guid ReportId, Guid PropertyId, string PropertyTitle, ReportReason Reason) : INotification;
