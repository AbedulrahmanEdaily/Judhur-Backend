using Judhur.Domain.Common.Results;
using Judhur.Domain.Notifications;
using Judhur.Domain.Notifications.Enums;

namespace Judhur.Tests.Common.Factories;

public static class NotificationFactory
{
    public const string DefaultTitle = "تمت الموافقة على عقارك";
    public const string DefaultBody = "أصبح عقارك ظاهرًا للجميع";

    public static readonly Guid DefaultUserId = PropertyFactory.DefaultSellerId;
    public static readonly Guid DefaultReferenceId = Guid.Parse("0199a1b2-0000-7000-8000-0000000000b3");

    public static Result<Notification> CreateResult(
        Guid? userId = null,
        NotificationType type = NotificationType.PropertyApproved,
        string title = DefaultTitle,
        string body = DefaultBody,
        Guid? referenceId = null,
        Guid? id = null)
        => Notification.Create(
            id ?? Guid.NewGuid(),
            userId ?? DefaultUserId,
            type,
            title,
            body,
            referenceId ?? DefaultReferenceId);

    public static Notification Create(
        Guid? userId = null,
        NotificationType type = NotificationType.PropertyApproved,
        string title = DefaultTitle,
        string body = DefaultBody,
        Guid? referenceId = null,
        Guid? id = null)
        => CreateResult(userId, type, title, body, referenceId, id).GetValueOrThrow();
}
