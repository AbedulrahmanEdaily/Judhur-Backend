using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Notifications.Dto;
using Judhur.Application.Features.Users.Events;
using Judhur.Domain.Notifications;
using Judhur.Domain.Notifications.Enums;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.EventHandlers;

public sealed class NotifyUserOnBannedHandler(IAppDbContext context, ILogger<NotifyUserOnBannedHandler> logger, IRealtimeNotifier realtimeNotifier) : INotificationHandler<UserBanned>
{
    private const string NotificationTitle = "تم حظر حسابك";

    private readonly IAppDbContext _context = context;
    private readonly ILogger<NotifyUserOnBannedHandler> _logger = logger;
    private readonly IRealtimeNotifier _realtimeNotifier = realtimeNotifier;

    public async Task Handle(UserBanned notification, CancellationToken cancellationToken)
    {
        var body = $"تم حظر حسابك من قبل إدارة جذور. السبب: {notification.Reason.TrimEnd('.', ' ')}.";
        if (notification.DeactivatedListings > 0)
        {
            body += $" تم إيقاف إعلاناتك ({notification.DeactivatedListings}).";
        }
        var notificationResult = Notification.Create(
            Guid.CreateVersion7(),
            notification.UserId,
            NotificationType.AccountBanned,
            NotificationTitle,
            body,
            null);
        if (notificationResult.IsError)
        {
            _logger.LogError(
                "Ban notification could not be created for user {UserId}: {ErrorCode}",
                notification.UserId,
                notificationResult.TopError.Code);
            return;
        }

        _context.Notifications.Add(notificationResult.Value);
        await _context.SaveChangesAsync(cancellationToken);
        await _realtimeNotifier.NotificationReceivedAsync(notificationResult.Value.UserId, notificationResult.Value.ToDto(), cancellationToken);
        _logger.LogInformation(
            "Notification {NotificationId} sent to banned user {UserId}",
            notificationResult.Value.Id,
            notification.UserId);
    }
}
