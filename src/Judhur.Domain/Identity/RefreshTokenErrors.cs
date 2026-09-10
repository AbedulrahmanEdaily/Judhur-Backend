using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Identity;

public static class RefreshTokenErrors
{
    public static readonly Error TokenRequired =
        Error.Validation("RefreshTokenErrors.TokenRequired", "قيمة التوكن مطلوبة");

    public static readonly Error TokenTooLong =
        Error.Validation("RefreshTokenErrors.TokenTooLong", $"لا يمكن أن يتجاوز التوكن {RefreshToken.MaxTokenLength} حرف");

    public static readonly Error UserIdRequired =
        Error.Validation("RefreshTokenErrors.UserIdRequired", "معرف المستخدم مطلوب");

    public static readonly Error ExpiryInvalid =
        Error.Validation("RefreshTokenErrors.ExpiryInvalid", "تاريخ الانتهاء يجب أن يكون في المستقبل");

    public static readonly Error Expired =
        Error.Unauthorized("RefreshTokenErrors.Expired", "انتهت صلاحية الجلسة، يرجى تسجيل الدخول مجددا");

    public static readonly Error NotFound =
        Error.NotFound("RefreshTokenErrors.NotFound", "التوكن غير موجود");
}
