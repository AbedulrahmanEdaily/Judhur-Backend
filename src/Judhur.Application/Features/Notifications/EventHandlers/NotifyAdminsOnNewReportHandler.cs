using Judhur.Application.Features.Reports.Events;
using Judhur.Domain.Notifications.Enums;
using Judhur.Domain.Reports.Enums;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.EventHandlers;

public sealed class NotifyAdminsOnNewReportHandler(AdminNotifications adminNotifications, ILogger<NotifyAdminsOnNewReportHandler> logger)
    : INotificationHandler<ReportCreated>
{
    private const string NotificationTitle = "بلاغ جديد";

    private readonly AdminNotifications _adminNotifications = adminNotifications;
    private readonly ILogger<NotifyAdminsOnNewReportHandler> _logger = logger;

    public async Task Handle(ReportCreated notification, CancellationToken cancellationToken)
    {
        var sent = await _adminNotifications.SendAsync(
            NotificationType.NewReport,
            NotificationTitle,
            $"وصل بلاغ جديد على العقار \"{notification.PropertyTitle}\" بسبب: {ReasonText(notification.Reason)}.",
            notification.ReportId,
            cancellationToken);
        _logger.LogInformation("Report {ReportId} created: {Count} admins notified", notification.ReportId, sent);
    }

    private static string ReasonText(ReportReason reason) => reason switch
    {
        ReportReason.Fraud => "احتيال",
        ReportReason.IncorrectInformation => "معلومات غير صحيحة",
        ReportReason.Duplicate => "إعلان مكرر",
        ReportReason.InappropriateContent => "محتوى غير لائق",
        _ => "سبب آخر"
    };
}
