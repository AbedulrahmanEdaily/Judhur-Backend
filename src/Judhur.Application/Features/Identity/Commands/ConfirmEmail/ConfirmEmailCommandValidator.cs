using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty().WithMessage("معرّف المستخدم مطلوب.");
        RuleFor(c => c.Token)
            .NotEmpty().WithMessage("رمز التأكيد مطلوب.");
    }
}