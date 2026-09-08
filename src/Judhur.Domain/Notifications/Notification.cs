using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Notifications.Enums;

namespace Judhur.Domain.Notifications;

public sealed class Notification : AuditableEntity
{
    public const int MaxTitleLength = 200;
    public const int MaxBodyLength = 1000;

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public Guid? ReferenceId { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public bool IsRead => ReadAtUtc is not null;

    private Notification() { }

    private Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        string title,
        string body,
        Guid? referenceId) : base(id)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
        ReferenceId = referenceId;
    }

    public static Result<Notification> Create(
        Guid id,
        Guid userId,
        NotificationType type,
        string title,
        string body,
        Guid? referenceId)
    {
        if (userId == Guid.Empty)
        {
            return NotificationErrors.UserIdRequired;
        }
        if (!Enum.IsDefined(type))
        {
            return NotificationErrors.InvalidType;
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            return NotificationErrors.TitleRequired;
        }
        if (title.Length > MaxTitleLength)
        {
            return NotificationErrors.TitleTooLong;
        }
        if (string.IsNullOrWhiteSpace(body))
        {
            return NotificationErrors.BodyRequired;
        }
        if (body.Length > MaxBodyLength)
        {
            return NotificationErrors.BodyTooLong;
        }
        return new Notification(id, userId, type, title, body, referenceId);
    }

    public void MarkAsRead(DateTimeOffset readAtUtc)
    {
        if (IsRead)
        {
            return;
        }
        ReadAtUtc = readAtUtc;
    }
}
