using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.ForgetPassword;

public sealed class ForgetPasswordCommandValidator : AbstractValidator<ForgetPasswordCommand>
{
    public ForgetPasswordCommandValidator()
    {
        RuleFor(f => f.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email cannot be null or empty")
            .EmailAddress().WithMessage("Email is not a valid email address");
    }
}