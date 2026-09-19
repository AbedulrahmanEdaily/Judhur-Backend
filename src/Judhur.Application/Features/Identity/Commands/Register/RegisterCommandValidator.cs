using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private const int MaxUserNameLength = 256;
    private const int MaxEmailLength = 256;
    private const int MaxFullNameLength = 150;
    private const int MaxCityLength = 100;
    private const int MinPasswordLength = 8;

    public RegisterCommandValidator()
    {
        RuleFor(r => r.UserName)
            .NotEmpty().WithMessage("UserName cannot be null or empty")
            .MaximumLength(MaxUserNameLength).WithMessage($"UserName cannot exceed {MaxUserNameLength} characters");

        RuleFor(r => r.FullName)
            .NotEmpty().WithMessage("FullName cannot be null or empty")
            .MaximumLength(MaxFullNameLength).WithMessage($"FullName cannot exceed {MaxFullNameLength} characters");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Email cannot be null or empty")
            .EmailAddress().WithMessage("Email is not a valid email address")
            .MaximumLength(MaxEmailLength).WithMessage($"Email cannot exceed {MaxEmailLength} characters");

        RuleFor(r => r.City)
            .NotEmpty().WithMessage("City cannot be null or empty")
            .MaximumLength(MaxCityLength).WithMessage($"City cannot exceed {MaxCityLength} characters");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Password cannot be null or empty")
            .MinimumLength(MinPasswordLength).WithMessage($"Password must be at least {MinPasswordLength} characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit");

        RuleFor(r => r.PhoneNumber)
            .NotEmpty().WithMessage("PhoneNumber cannot be null or empty")
            .Matches(@"^(?:\+?(?:970|972)\d{9}|05\d{8})$").WithMessage("Invalid PhoneNumber");
    }
}
