using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Notifications.Dto;
using Judhur.Domain.Notifications;
using Judhur.Domain.Notifications.Enums;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications;

public sealed class AdminNotifications(IAppDbContext context, IIdentityService identityService, IRealtimeNotifier realtimeNotifier, ILogger<AdminNotifications> logger)
{
    private readonly IAppDbContext _context = context;
    private readonly IIdentityService _identityService = identityService;
    private readonly IRealtimeNotifier _realtimeNotifier = realtimeNotifier;
    private readonly ILogger<AdminNotifications> _logger = logger;

    public async Task<int> SendAsync(NotificationType type, string title, string body, Guid referenceId, CancellationToken cancellationToken)
    {
        var adminIds = await _identityService.GetAdminIdsAsync(cancellationToken);
        var notifications = new List<Notification>(adminIds.Count);
        foreach (var adminId in adminIds)
        {
            var result = Notification.Create(Guid.CreateVersion7(), adminId, type, title, body, referenceId);
            if (result.IsError)
            {
                _logger.LogError("{Type} notification could not be created for admin {AdminId}: {ErrorCode}", type, adminId, result.TopError.Code);
                continue;
            }
            notifications.Add(result.Value);
        }
        if (notifications.Count == 0)
        {
            return 0;
        }

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            await _realtimeNotifier.NotificationReceivedAsync(notification.UserId, notification.ToDto(), cancellationToken);
        }
        return notifications.Count;
    }
}
