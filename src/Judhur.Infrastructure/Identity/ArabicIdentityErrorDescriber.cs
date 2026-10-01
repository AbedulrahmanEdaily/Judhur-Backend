using Microsoft.AspNetCore.Identity;

namespace Judhur.Infrastructure.Identity;

/// <summary>
/// Arabic messages for the errors ASP.NET Core Identity produces itself
/// (duplicate email, weak password, ...). Codes stay the same as the defaults.
/// </summary>
public sealed class ArabicIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
        => new() { Code = nameof(DefaultError), Description = "حدث خطأ غير متوقع، يرجى المحاولة لاحقًا." };

    public override IdentityError ConcurrencyFailure()
        => new() { Code = nameof(ConcurrencyFailure), Description = "تم تعديل البيانات من طلب آخر، يرجى المحاولة مجددًا." };

    public override IdentityError InvalidToken()
        => new() { Code = nameof(InvalidToken), Description = "الرمز غير صالح أو منتهي الصلاحية." };

    public override IdentityError PasswordMismatch()
        => new() { Code = nameof(PasswordMismatch), Description = "كلمة المرور الحالية غير صحيحة." };

    public override IdentityError InvalidUserName(string? userName)
        => new() { Code = nameof(InvalidUserName), Description = "اسم المستخدم غير صالح، يمكن أن يحتوي على أحرف إنجليزية وأرقام فقط." };

    public override IdentityError InvalidEmail(string? email)
        => new() { Code = nameof(InvalidEmail), Description = "صيغة البريد الإلكتروني غير صحيحة." };

    public override IdentityError DuplicateUserName(string userName)
        => new() { Code = nameof(DuplicateUserName), Description = "اسم المستخدم مستخدم بالفعل." };

    public override IdentityError DuplicateEmail(string email)
        => new() { Code = nameof(DuplicateEmail), Description = "البريد الإلكتروني مسجّل بالفعل." };

    public override IdentityError PasswordTooShort(int length)
        => new() { Code = nameof(PasswordTooShort), Description = $"يجب ألا تقل كلمة المرور عن {length} أحرف." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"يجب أن تحتوي كلمة المرور على {uniqueChars} أحرف مختلفة على الأقل." };

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "يجب أن تحتوي كلمة المرور على رمز خاص واحد على الأقل." };

    public override IdentityError PasswordRequiresDigit()
        => new() { Code = nameof(PasswordRequiresDigit), Description = "يجب أن تحتوي كلمة المرور على رقم واحد على الأقل." };

    public override IdentityError PasswordRequiresLower()
        => new() { Code = nameof(PasswordRequiresLower), Description = "يجب أن تحتوي كلمة المرور على حرف إنجليزي صغير واحد على الأقل." };

    public override IdentityError PasswordRequiresUpper()
        => new() { Code = nameof(PasswordRequiresUpper), Description = "يجب أن تحتوي كلمة المرور على حرف إنجليزي كبير واحد على الأقل." };
}
