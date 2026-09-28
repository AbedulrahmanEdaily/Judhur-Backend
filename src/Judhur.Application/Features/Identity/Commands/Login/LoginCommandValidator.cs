using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(l => l.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.");

        RuleFor(l => l.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة.");
    }
}
