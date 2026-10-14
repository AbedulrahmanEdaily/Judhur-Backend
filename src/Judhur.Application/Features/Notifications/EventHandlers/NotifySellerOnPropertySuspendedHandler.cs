using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Events;
using Judhur.Domain.Notifications;
using Judhur.Domain.Notifications.Enums;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.EventHandlers;

public sealed class NotifySellerOnPropertySuspendedHandler(IAppDbContext context, ILogger<NotifySellerOnPropertySuspendedHandler> logger) : INotificationHandler<PropertySuspended>
{
    private const string NotificationTitle = "تم إيقاف عقارك";

    private readonly IAppDbContext _context = context;
    private readonly ILogger<NotifySellerOnPropertySuspendedHandler> _logger = logger;

    public async Task Handle(PropertySuspended notification, CancellationToken cancellationToken)
    {
        var notificationResult = Notification.Create(
            Guid.CreateVersion7(),
            notification.SellerId,
            NotificationType.PropertySuspended,
            NotificationTitle,
            $"تم إيقاف عقارك «{notification.Title}» وإخفاؤه من نتائج البحث بعد مراجعة الإدارة. السبب: {notification.Reason.TrimEnd('.', ' ')}. عدّل العقار ثم أعد إرساله للمراجعة.",
            notification.PropertyId);
        if (notificationResult.IsError)
        {
            _logger.LogError(
                "Notification for suspended property {PropertyId} could not be created for seller {SellerId}: {ErrorCode}",
                notification.PropertyId,
                notification.SellerId,
                notificationResult.TopError.Code);
            return;
        }

        _context.Notifications.Add(notificationResult.Value);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Notification {NotificationId} sent to seller {SellerId} for suspended property {PropertyId}",
            notificationResult.Value.Id,
            notification.SellerId,
            notification.PropertyId);
    }
}
