using Judhur.Domain.Notifications;

namespace Judhur.Application.Features.Notifications.Dto;

public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type,
        Title = notification.Title,
        Body = notification.Body,
        ReferenceId = notification.ReferenceId,
        IsRead = notification.ReadAtUtc != null,
        CreatedAtUtc = notification.CreatedAtUtc
    };
}
