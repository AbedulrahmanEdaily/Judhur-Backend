using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reports.Events;
using Judhur.Domain.Notifications;
using Judhur.Domain.Notifications.Enums;
using Judhur.Domain.Reports.Enums;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.EventHandlers;

public sealed class NotifyReporterOnReportReviewedHandler(IAppDbContext context, ILogger<NotifyReporterOnReportReviewedHandler> logger) : INotificationHandler<ReportReviewed>
{
    private const string NotificationTitle = "تمت مراجعة بلاغك";

    private readonly IAppDbContext _context = context;
    private readonly ILogger<NotifyReporterOnReportReviewedHandler> _logger = logger;

    public async Task Handle(ReportReviewed notification, CancellationToken cancellationToken)
    {
        var outcome = notification.Outcome == ReportStatus.Resolved
            ? $"راجعنا بلاغك عن العقار «{notification.PropertyTitle}» واتخذنا الإجراء المناسب. شكرًا لمساعدتك في الحفاظ على موثوقية جذور."
            : $"راجعنا بلاغك عن العقار «{notification.PropertyTitle}» ولم نجد فيه ما يخالف شروط النشر.";
        var body = notification.AdminNote is { } note
            ? $"{outcome} ملاحظة الإدارة: {note}"
            : outcome;
        if (body.Length > Notification.MaxBodyLength)
        {
            var cut = Notification.MaxBodyLength - 1;
            if (char.IsHighSurrogate(body[cut - 1]))
            {
                cut--;
            }
            body = string.Concat(body.AsSpan(0, cut), "…");
        }

        var notificationResult = Notification.Create(
            Guid.CreateVersion7(),
            notification.ReporterId,
            NotificationType.ReportResolved,
            NotificationTitle,
            body,
            notification.ReportId);
        if (notificationResult.IsError)
        {
            _logger.LogError(
                "Notification for reviewed report {ReportId} could not be created for reporter {ReporterId}: {ErrorCode}",
                notification.ReportId,
                notification.ReporterId,
                notificationResult.TopError.Code);
            return;
        }

        _context.Notifications.Add(notificationResult.Value);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Notification {NotificationId} sent to reporter {ReporterId} for report {ReportId} ({Outcome})",
            notificationResult.Value.Id,
            notification.ReporterId,
            notification.ReportId,
            notification.Outcome);
    }
}
