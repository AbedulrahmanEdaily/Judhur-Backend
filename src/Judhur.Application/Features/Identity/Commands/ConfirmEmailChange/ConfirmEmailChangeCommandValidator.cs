using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.ConfirmEmailChange;

public sealed class ConfirmEmailChangeCommandValidator : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty().WithMessage("معرّف المستخدم مطلوب.");

        RuleFor(c => c.NewEmail)
            .NotEmpty().WithMessage("البريد الإلكتروني الجديد مطلوب.")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.");

        RuleFor(c => c.Token)
            .NotEmpty().WithMessage("رمز التأكيد مطلوب.");
    }
}
