using FluentValidation;

using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.Features.Properties.Commands.CreateProperty;

public sealed class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(p => p.Title)
            .NotEmpty().WithMessage("العنوان مطلوب.")
            .MaximumLength(Property.MaxTitleLength).WithMessage($"لا يمكن أن يتجاوز العنوان {Property.MaxTitleLength} حرف.");

        RuleFor(p => p.Description)
            .MaximumLength(Property.MaxDescriptionLength).WithMessage($"لا يمكن أن يتجاوز الوصف {Property.MaxDescriptionLength} حرف.");

        RuleFor(p => p.Price)
            .GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من صفر.")
            .LessThanOrEqualTo(Property.MaxPrice).WithMessage("السعر أكبر من الحد المسموح.");

        RuleFor(p => p.PaymentType).IsInEnum().WithMessage("طريقة الدفع غير صالحة.");
        RuleFor(p => p.PropertyType).IsInEnum().WithMessage("نوع العقار غير صالح.");

        RuleFor(p => p.PropertyStatus)
            .Must(status => status is PropertyStatus.ForSale or PropertyStatus.ForRent)
            .WithMessage("عند إنشاء العرض يجب أن تكون حالته للبيع أو للإيجار فقط.");

        RuleFor(p => p.Area)
            .GreaterThan(0).WithMessage("المساحة يجب أن تكون أكبر من صفر.")
            .LessThanOrEqualTo(Property.MaxArea).WithMessage("المساحة أكبر من الحد المسموح.");

        RuleFor(p => p.City)
            .NotEmpty().WithMessage("المدينة مطلوبة.")
            .MaximumLength(Property.MaxCityLength).WithMessage($"لا يمكن أن يتجاوز اسم المدينة {Property.MaxCityLength} حرف.");

        RuleFor(p => p.Region)
            .MaximumLength(Property.MaxRegionLength).WithMessage($"لا يمكن أن يتجاوز اسم المنطقة {Property.MaxRegionLength} حرف.");

        RuleFor(p => p.FullAddress)
            .NotEmpty().WithMessage("العنوان الكامل مطلوب.")
            .MaximumLength(Property.MaxFullAddressLength).WithMessage($"لا يمكن أن يتجاوز العنوان الكامل {Property.MaxFullAddressLength} حرف.");

        RuleFor(p => p.Latitude).InclusiveBetween(-90, 90).WithMessage("يجب أن يكون خط العرض بين -90 و90.");
        RuleFor(p => p.Longitude).InclusiveBetween(-180, 180).WithMessage("يجب أن يكون خط الطول بين -180 و180.");

        RuleFor(p => p.LandClassification).IsInEnum().WithMessage("تصنيف الأرض غير صالح.");
        RuleFor(p => p.LegalStatus).IsInEnum().WithMessage("الوضع القانوني للعقار غير صالح.");
    }
}