using Judhur.Domain.Common.Results;

namespace Judhur.Application.Common;

public static class ApplicationError
{
    public static readonly Error ExpiredAccessTokenInvalid = Error.Unauthorized(
        code: "Auth.ExpiredAccessToken.Invalid",
        description: "الجلسة غير صالحة، يرجى تسجيل الدخول مجددًا.");
    public static readonly Error UserIdClaimInvalid = Error.Unauthorized(
        code: "Auth.UserIdClaim.Invalid",
        description: "بيانات الجلسة غير صالحة، يرجى تسجيل الدخول مجددًا.");
    public static readonly Error InvalidGoogleToken = Error.Unauthorized(
        code: "Identity.InvalidGoogleToken",
        description: "تعذّر التحقق من حساب Google، يرجى المحاولة مجددًا.");
    public static readonly Error UserNotFound = Error.NotFound(
        code: "Auth.User.NotFound",
        description: "المستخدم غير موجود.");
    public static readonly Error AccountBanned = Error.Forbidden(
        code: "Identity.AccountBanned",
        description: "تم حظر حسابك. للاستفسار تواصل مع إدارة جذور.");
    public static readonly Error PropertyChangedSinceReview = Error.Conflict(
        code: "Properties.ChangedSinceReview",
        description: "تم تعديل العقار بعد فتحك لصفحة المراجعة. حدّث الصفحة وراجعه من جديد.");
    public static readonly Error AdminCannotDeleteAccount = Error.Conflict(
        code: "Users.AdminCannotDeleteAccount",
        description: "لا يمكن حذف حساب مشرف.");
    public static readonly Error InvalidPassword = Error.Validation(
        code: "Identity.InvalidPassword",
        description: "كلمة المرور غير صحيحة.");
    public static readonly Error ParticipantUnavailable = Error.Conflict(
        code: "Conversations.ParticipantUnavailable",
        description: "لم يعد بإمكانك مراسلة هذا المستخدم.");
    public static readonly Error CannotBanAdmin = Error.Conflict(
        code: "Users.CannotBanAdmin",
        description: "لا يمكن حظر حساب مشرف.");
    public static readonly Error CannotBanSelf = Error.Conflict(
        code: "Users.CannotBanSelf",
        description: "لا يمكنك حظر حسابك.");
    public static readonly Error UserAlreadyBanned = Error.Conflict(
        code: "Users.AlreadyBanned",
        description: "هذا الحساب محظور مسبقًا.");
    public static readonly Error UserNotBanned = Error.Conflict(
        code: "Users.NotBanned",
        description: "هذا الحساب غير محظور.");
    public static readonly Error SellerNotFound = Error.NotFound(
        code: "Seller.NotFound",
        description: "البائع غير موجود.");
    public static readonly Error TokenGenerationFailed = Error.Failure(
        code: "Auth.TokenGeneration.Failed",
        description: "تعذّر إنشاء رمز الدخول، يرجى المحاولة لاحقًا.");
    public static readonly Error Unauthenticated = Error.Unauthorized(
        code: "Auth.NotAuthenticated",
        description: "يجب تسجيل الدخول لتنفيذ هذا الإجراء.");
    public static readonly Error UploadFailed = Error.Failure(
        code: "Storage.UploadFailed",
        description: "تعذّر رفع الملف، يرجى المحاولة لاحقًا.");

    public static readonly Error InvalidFileContent = Error.Validation(
        code: "Storage.InvalidFileContent",
        description: "محتوى الملف لا يطابق صيغته، يرجى رفع ملف صحيح.");

    public static readonly Error DeleteFailed = Error.Failure(
        code: "Storage.DeleteFailed",
        description: "تعذّر حذف الملف، يرجى المحاولة لاحقًا.");

    public static readonly Error PageInvalid = Error.Validation(
        code: "Pagination.PageInvalid",
        description: "رقم الصفحة يجب أن يكون أكبر من صفر.");

    public static readonly Error PageSizeInvalid = Error.Validation(
        code: "Pagination.PageSizeInvalid",
        description: "حجم الصفحة يجب أن يكون بين 1 و100.");
}
