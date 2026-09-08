using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Notifications;

public static class NotificationErrors
{
    public static readonly Error UserIdRequired = Error.Validation("NotificationErrors.UserIdRequired", "معرف المستخدم مطلوب");
    public static readonly Error InvalidType = Error.Validation("NotificationErrors.InvalidType", "نوع الإشعار غير صالح");
    public static readonly Error TitleRequired = Error.Validation("NotificationErrors.TitleRequired", "عنوان الإشعار مطلوب");
    public static readonly Error TitleTooLong = Error.Validation("NotificationErrors.TitleTooLong", $"لا يمكن أن يتجاوز عنوان الإشعار {Notification.MaxTitleLength} حرف");
    public static readonly Error BodyRequired = Error.Validation("NotificationErrors.BodyRequired", "نص الإشعار مطلوب");
    public static readonly Error BodyTooLong = Error.Validation("NotificationErrors.BodyTooLong", $"لا يمكن أن يتجاوز نص الإشعار {Notification.MaxBodyLength} حرف");
}
