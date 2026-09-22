using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(r => r.RefreshToken)
            .NotEmpty().WithMessage("Refresh token required.");

        RuleFor(r => r.ExpiredAccessToken)
            .NotEmpty().WithMessage("Access token required.");
    }
}
