using FluentValidation;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private const int MaxUserNameLength = 256;
    private const int MaxEmailLength = 256;
    private const int MaxFullNameLength = 150;
    private const int MaxCityLength = 100;
    private const int MinPasswordLength = 8;
    private const int MaxProfileImageUrlLength = 500;
    private const int MaxBioLength = 1000;

    public RegisterCommandValidator()
    {
        RuleFor(r => r.UserName)
            .NotEmpty().WithMessage("اسم المستخدم مطلوب.")
            .MaximumLength(MaxUserNameLength).WithMessage($"لا يمكن أن يتجاوز اسم المستخدم {MaxUserNameLength} حرف.");

        RuleFor(r => r.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب.")
            .MaximumLength(MaxFullNameLength).WithMessage($"لا يمكن أن يتجاوز الاسم الكامل {MaxFullNameLength} حرف.");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.")
            .MaximumLength(MaxEmailLength).WithMessage($"لا يمكن أن يتجاوز البريد الإلكتروني {MaxEmailLength} حرف.");

        RuleFor(r => r.City)
            .NotEmpty().WithMessage("المدينة مطلوبة.")
            .MaximumLength(MaxCityLength).WithMessage($"لا يمكن أن يتجاوز اسم المدينة {MaxCityLength} حرف.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة.")
            .MinimumLength(MinPasswordLength).WithMessage($"يجب ألا تقل كلمة المرور عن {MinPasswordLength} أحرف.")
            .Matches("[A-Z]").WithMessage("يجب أن تحتوي كلمة المرور على حرف إنجليزي كبير واحد على الأقل.")
            .Matches("[a-z]").WithMessage("يجب أن تحتوي كلمة المرور على حرف إنجليزي صغير واحد على الأقل.")
            .Matches("[0-9]").WithMessage("يجب أن تحتوي كلمة المرور على رقم واحد على الأقل.");

        RuleFor(r => r.PhoneNumber)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب.")
            .Matches(@"^(?:\+?(?:970|972)\d{9}|05\d{8})$").WithMessage("رقم الهاتف غير صالح.");

        RuleFor(r => r.Bio)
        .MaximumLength(MaxBioLength).WithMessage($"لا يمكن أن تتجاوز النبذة التعريفية {MaxBioLength} حرف.");

        RuleFor(r => r.ProfileImageUrl)
        .MaximumLength(MaxProfileImageUrlLength).WithMessage($"لا يمكن أن يتجاوز رابط صورة الملف الشخصي {MaxProfileImageUrlLength} حرف.");
    }
}
