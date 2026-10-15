using System.Globalization;

using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Notifications.Dto;
using Judhur.Application.Features.Reviews.Events;
using Judhur.Domain.Notifications;
using Judhur.Domain.Notifications.Enums;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.EventHandlers;

public sealed class NotifySellerOnNewReviewHandler(IAppDbContext context, ILogger<NotifySellerOnNewReviewHandler> logger, IRealtimeNotifier realtimeNotifier) : INotificationHandler<ReviewCreated>
{
    private const string NotificationTitle = "حصلت على تقييم جديد";

    private readonly IAppDbContext _context = context;
    private readonly ILogger<NotifySellerOnNewReviewHandler> _logger = logger;
    private readonly IRealtimeNotifier _realtimeNotifier = realtimeNotifier;

    public async Task Handle(ReviewCreated notification, CancellationToken cancellationToken)
    {
        var rating = notification.Rating.ToString("0.#", CultureInfo.InvariantCulture);
        var notificationResult = Notification.Create(
            Guid.CreateVersion7(),
            notification.SellerId,
            NotificationType.NewReview,
            NotificationTitle,
            $"قيّمك أحد المشترين بـ {rating} من 5. يظهر التقييم في صفحتك العامة.",
            notification.SellerId);
        if (notificationResult.IsError)
        {
            _logger.LogError(
                "Notification for review {ReviewId} could not be created for seller {SellerId}: {ErrorCode}",
                notification.ReviewId,
                notification.SellerId,
                notificationResult.TopError.Code);
            return;
        }

        _context.Notifications.Add(notificationResult.Value);
        await _context.SaveChangesAsync(cancellationToken);
        await _realtimeNotifier.NotificationReceivedAsync(notificationResult.Value.UserId, notificationResult.Value.ToDto(), cancellationToken);
        _logger.LogInformation(
            "Notification {NotificationId} sent to seller {SellerId} for review {ReviewId}",
            notificationResult.Value.Id,
            notification.SellerId,
            notification.ReviewId);
    }
}
