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
    public static readonly Error UserNotFound = Error.NotFound(
        code: "Auth.User.NotFound",
        description: "المستخدم غير موجود.");
    public static readonly Error TokenGenerationFailed = Error.Failure(
        code: "Auth.TokenGeneration.Failed",
        description: "تعذّر إنشاء رمز الدخول، يرجى المحاولة لاحقًا.");
    public static readonly Error Unauthenticated = Error.Unauthorized(
        code: "Auth.NotAuthenticated",
        description: "يجب تسجيل الدخول لتنفيذ هذا الإجراء.");
    public static readonly Error UploadFailed = Error.Failure(
        code: "Storage.UploadFailed",
        description: "تعذّر رفع الملف، يرجى المحاولة لاحقًا.");

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
