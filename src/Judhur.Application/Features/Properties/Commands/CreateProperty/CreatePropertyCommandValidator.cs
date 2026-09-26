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
            .MaximumLength(Property.MaxTitleLength);

        RuleFor(p => p.Description)
            .MaximumLength(Property.MaxDescriptionLength);

        RuleFor(p => p.Price)
            .GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من صفر.");

        RuleFor(p => p.PaymentType).IsInEnum();
        RuleFor(p => p.PropertyType).IsInEnum();

        RuleFor(p => p.PropertyStatus)
            .Must(status => status is PropertyStatus.ForSale or PropertyStatus.ForRent)
            .WithMessage("عند إنشاء العرض يجب أن تكون حالته للبيع أو للإيجار فقط.");

        RuleFor(p => p.Area)
            .GreaterThan(0).WithMessage("المساحة يجب أن تكون أكبر من صفر.");

        RuleFor(p => p.City)
            .NotEmpty().WithMessage("المدينة مطلوبة.")
            .MaximumLength(Property.MaxCityLength);

        RuleFor(p => p.Region)
            .MaximumLength(Property.MaxRegionLength);

        RuleFor(p => p.FullAddress)
            .NotEmpty().WithMessage("العنوان الكامل مطلوب.")
            .MaximumLength(Property.MaxFullAddressLength);

        RuleFor(p => p.Latitude).InclusiveBetween(-90, 90);
        RuleFor(p => p.Longitude).InclusiveBetween(-180, 180);

        RuleFor(p => p.LandClassification).IsInEnum();
        RuleFor(p => p.LegalStatus).IsInEnum();

        RuleFor(p => p.OwnershipDocumentUrl)
            .NotEmpty().WithMessage("وثيقة الملكية مطلوبة.")
            .MaximumLength(Property.MaxOwnershipDocumentUrlLength);
    }
}