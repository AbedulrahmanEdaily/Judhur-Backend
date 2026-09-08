
using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Users;

public static class UserErrors
{
    public static readonly Error NameRequired = Error.Validation("UserErrors.NameRequired", "الاسم مطلوب");
    public static readonly Error EmailRequired = Error.Validation("UserErrors.EmailRequired", "الايميل مطلوب");
    public static readonly Error InvalidPhoneNumber = Error.Validation("UserErrors.InvalidPhoneNumber", "رقم الهاتف غير صحيح");
    public static readonly Error InvalidUserRole = Error.Validation("UserErrors.InvalidUserRole", "الدور غير صالح");
    public static readonly Error InvalidEmail = Error.Validation("UserErrors.InvalidEmail", "الايميل غير صحيح");
    public static readonly Error UserIsAlreadyBanned = Error.Conflict("UserErrors.UserIsAlreadyBanned", "المستخدم محظور");
    public static readonly Error UserIsAlreadyUnBanned = Error.Conflict("UserErrors.UserIsAlreadyUnBanned", "المستخدم غير محظور");
    public static readonly Error CannotBanSelf = Error.Conflict("UserErrors.CannotBanSelf", "لا تستطيع حظر نفسك");
    public static readonly Error BanReasonRequired = Error.Validation("UserErrors.BanReasonRequired", "سبب الحظر مطلوب");
}