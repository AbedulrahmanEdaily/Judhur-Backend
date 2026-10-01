using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.SetMyPassword;

public sealed class SetMyPasswordCommandValidator : AbstractValidator<SetMyPasswordCommand>
{
    private const int MinPasswordLength = 8;

    public SetMyPasswordCommandValidator()
    {
        RuleFor(r => r.NewPassword)
            .NotEmpty().WithMessage("كلمة المرور الجديدة مطلوبة.")
            .MinimumLength(MinPasswordLength).WithMessage($"يجب ألا تقل كلمة المرور عن {MinPasswordLength} أحرف.")
            .Matches("[A-Z]").WithMessage("يجب أن تحتوي كلمة المرور على حرف إنجليزي كبير واحد على الأقل.")
            .Matches("[a-z]").WithMessage("يجب أن تحتوي كلمة المرور على حرف إنجليزي صغير واحد على الأقل.")
            .Matches("[0-9]").WithMessage("يجب أن تحتوي كلمة المرور على رقم واحد على الأقل.")
            .Must((r, newPassword) => newPassword != r.CurrentPassword).WithMessage("يجب أن تختلف كلمة المرور الجديدة عن الحالية.");
    }
}
