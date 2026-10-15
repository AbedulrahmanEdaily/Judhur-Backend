using Judhur.Application.Features.Properties.Events;
using Judhur.Domain.Notifications.Enums;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.EventHandlers;

public sealed class NotifyAdminsOnPropertyQueuedHandler(AdminNotifications adminNotifications, ILogger<NotifyAdminsOnPropertyQueuedHandler> logger)
    : INotificationHandler<PropertyQueuedForReview>
{
    private const string NotificationTitle = "عقار بانتظار المراجعة";

    private readonly AdminNotifications _adminNotifications = adminNotifications;
    private readonly ILogger<NotifyAdminsOnPropertyQueuedHandler> _logger = logger;

    public async Task Handle(PropertyQueuedForReview notification, CancellationToken cancellationToken)
    {
        var sent = await _adminNotifications.SendAsync(
            NotificationType.PropertyPendingReview,
            NotificationTitle,
            $"العقار \"{notification.Title}\" جاهز للمراجعة.",
            notification.PropertyId,
            cancellationToken);
        _logger.LogInformation("Property {PropertyId} entered the review queue: {Count} admins notified", notification.PropertyId, sent);
    }
}
