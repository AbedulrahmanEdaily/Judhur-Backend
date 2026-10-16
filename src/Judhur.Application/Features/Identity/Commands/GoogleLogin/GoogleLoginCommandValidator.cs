using FluentValidation;

using Judhur.Application.Common;

namespace Judhur.Application.Features.Identity.Commands.GoogleLogin;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    private const int MaxCityLength = 100;

    public GoogleLoginCommandValidator()
    {
        RuleFor(c => c.IdToken)
            .NotEmpty().WithMessage("رمز Google مطلوب.");

        RuleFor(c => c.PhoneNumber)
            .Matches(PhoneNumbers.Pattern).WithMessage("رقم الهاتف غير صالح.")
            .When(c => !string.IsNullOrWhiteSpace(c.PhoneNumber));

        RuleFor(c => c.City)
            .MaximumLength(MaxCityLength).WithMessage($"لا يمكن أن يتجاوز اسم المدينة {MaxCityLength} حرف.")
            .When(c => !string.IsNullOrWhiteSpace(c.City));
    }
}
