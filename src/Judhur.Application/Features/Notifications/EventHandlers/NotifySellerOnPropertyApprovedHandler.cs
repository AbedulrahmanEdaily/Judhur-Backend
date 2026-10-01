using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Events;
using Judhur.Domain.Notifications;
using Judhur.Domain.Notifications.Enums;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.EventHandlers;

public sealed class NotifySellerOnPropertyApprovedHandler(IAppDbContext context, ILogger<NotifySellerOnPropertyApprovedHandler> logger) : INotificationHandler<PropertyApproved>
{
    private const string NotificationTitle = "تمت الموافقة على عقارك";

    private readonly IAppDbContext _context = context;
    private readonly ILogger<NotifySellerOnPropertyApprovedHandler> _logger = logger;

    public async Task Handle(PropertyApproved notification, CancellationToken cancellationToken)
    {
        var notificationResult = Notification.Create(
            Guid.CreateVersion7(),
            notification.SellerId,
            NotificationType.PropertyApproved,
            NotificationTitle,
            $"عقارك «{notification.Title}» أصبح منشوراً ويظهر الآن في نتائج البحث.",
            notification.PropertyId);
        if (notificationResult.IsError)
        {
            _logger.LogError(
                "Notification for approved property {PropertyId} could not be created for seller {SellerId}: {ErrorCode}",
                notification.PropertyId,
                notification.SellerId,
                notificationResult.TopError.Code);
            return;
        }

        _context.Notifications.Add(notificationResult.Value);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Notification {NotificationId} sent to seller {SellerId} for approved property {PropertyId}",
            notificationResult.Value.Id,
            notification.SellerId,
            notification.PropertyId);
    }
}
