using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.RequestEmailChange;

public sealed class RequestEmailChangeCommandValidator : AbstractValidator<RequestEmailChangeCommand>
{
    private const int MaxEmailLength = 256;

    public RequestEmailChangeCommandValidator()
    {
        RuleFor(r => r.NewEmail)
            .NotEmpty().WithMessage("البريد الإلكتروني الجديد مطلوب.")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.")
            .MaximumLength(MaxEmailLength).WithMessage($"لا يمكن أن يتجاوز البريد الإلكتروني {MaxEmailLength} حرف.");
    }
}
