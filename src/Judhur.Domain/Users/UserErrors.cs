using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Users;

public static class UserErrors
{
    public static readonly Error NameRequired =
        Error.Validation("UserErrors.NameRequired", "الاسم مطلوب");

    public static readonly Error NameTooLong =
        Error.Validation("UserErrors.NameTooLong", $"لا يمكن أن يتجاوز الاسم {User.MaxNameLength} حرف");

    public static readonly Error InvalidPhoneNumber =
        Error.Validation("UserErrors.InvalidPhoneNumber", "رقم الهاتف غير صحيح");

    public static readonly Error InvalidUserRole =
        Error.Validation("UserErrors.InvalidUserRole", "الدور غير صالح");

    public static readonly Error AdminIdRequired =
        Error.Validation("UserErrors.AdminIdRequired", "معرف المشرف مطلوب");

    public static readonly Error BanReasonRequired =
        Error.Validation("UserErrors.BanReasonRequired", "سبب الحظر مطلوب");

    public static readonly Error BanReasonTooLong =
        Error.Validation("UserErrors.BanReasonTooLong", $"لا يمكن أن يتجاوز سبب الحظر {User.MaxBanReasonLength} حرف");

    public static readonly Error UserIsAlreadyBanned =
        Error.Conflict("UserErrors.UserIsAlreadyBanned", "المستخدم محظور");

    public static readonly Error UserIsAlreadyUnBanned =
        Error.Conflict("UserErrors.UserIsAlreadyUnBanned", "المستخدم غير محظور");

    public static readonly Error CannotBanSelf =
        Error.Conflict("UserErrors.CannotBanSelf", "لا تستطيع حظر نفسك");

    public static readonly Error NotFound =
        Error.NotFound("UserErrors.NotFound", "المستخدم غير موجود");
}
