using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty().WithMessage("UserId cannot be empty");
        RuleFor(c => c.Token)
            .NotEmpty().WithMessage("Token cannot be null or empty");
    }
}