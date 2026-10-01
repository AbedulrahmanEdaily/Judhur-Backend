using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    private const int MaxFullNameLength = 150;
    private const int MaxCityLength = 100;
    private const int MaxBioLength = 1000;

    public UpdateMyProfileCommandValidator()
    {
        RuleFor(r => r.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب.")
            .MaximumLength(MaxFullNameLength).WithMessage($"لا يمكن أن يتجاوز الاسم الكامل {MaxFullNameLength} حرف.");

        RuleFor(r => r.PhoneNumber)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب.")
            .Matches(@"^(?:\+?(?:970|972)\d{9}|05\d{8})$").WithMessage("رقم الهاتف غير صالح.");

        RuleFor(r => r.City)
            .NotEmpty().WithMessage("المدينة مطلوبة.")
            .MaximumLength(MaxCityLength).WithMessage($"لا يمكن أن يتجاوز اسم المدينة {MaxCityLength} حرف.");

        RuleFor(r => r.Bio)
            .MaximumLength(MaxBioLength).WithMessage($"لا يمكن أن تتجاوز النبذة التعريفية {MaxBioLength} حرف.");
    }
}
