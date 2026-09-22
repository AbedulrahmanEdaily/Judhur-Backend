using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    private const int MinPasswordLength = 8;

    public ChangePasswordCommandValidator()
    {
        RuleFor(r => r.Code)
            .NotEmpty().WithMessage("Reset password code required.");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Email required.")
            .EmailAddress().WithMessage("Email is not a valid email address.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Password cannot be null or empty")
            .MinimumLength(MinPasswordLength).WithMessage($"Password must be at least {MinPasswordLength} characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit");
    }
}
