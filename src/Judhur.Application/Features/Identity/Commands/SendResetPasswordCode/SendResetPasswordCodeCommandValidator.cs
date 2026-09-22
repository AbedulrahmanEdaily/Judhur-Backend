using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.SendResetPasswordCode;

public sealed class SendResetPasswordCodeCommandValidator : AbstractValidator<SendResetPasswordCodeCommand>
{
    public SendResetPasswordCodeCommandValidator()
    {
        RuleFor(f => f.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email cannot be null or empty")
            .EmailAddress().WithMessage("Email is not a valid email address");
    }
}
