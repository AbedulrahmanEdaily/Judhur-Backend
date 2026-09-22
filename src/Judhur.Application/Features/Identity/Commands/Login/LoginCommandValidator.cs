using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(l => l.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email cannot be null or empty")
            .EmailAddress().WithMessage("Email is not a valid email address");

        RuleFor(l => l.Password)
            .NotEmpty().WithMessage("Password cannot be null or empty");
    }
}
