using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    private const int MinPasswordLength = 8;

    public ChangePasswordCommandValidator()
    {
        RuleFor(r => r.Code)
            .NotEmpty().WithMessage("رمز استعادة كلمة المرور مطلوب.");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة.")
            .MinimumLength(MinPasswordLength).WithMessage($"يجب ألا تقل كلمة المرور عن {MinPasswordLength} أحرف.")
            .Matches("[A-Z]").WithMessage("يجب أن تحتوي كلمة المرور على حرف إنجليزي كبير واحد على الأقل.")
            .Matches("[a-z]").WithMessage("يجب أن تحتوي كلمة المرور على حرف إنجليزي صغير واحد على الأقل.")
            .Matches("[0-9]").WithMessage("يجب أن تحتوي كلمة المرور على رقم واحد على الأقل.");
    }
}
