using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.SendResetPasswordCode;

public sealed class SendResetPasswordCodeCommandValidator : AbstractValidator<SendResetPasswordCodeCommand>
{
    public SendResetPasswordCodeCommandValidator()
    {
        RuleFor(f => f.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.");
    }
}
