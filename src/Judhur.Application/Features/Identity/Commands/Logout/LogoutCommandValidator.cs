using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(r => r.RefreshToken)
            .NotEmpty().WithMessage("رمز التحديث مطلوب.");
    }
}
