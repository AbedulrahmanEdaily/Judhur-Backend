using ContractsEnums = Judhur.Contracts.Common;
using DomainEnums = Judhur.Domain.Properties.Enums;

namespace Judhur.Api.Mapping;

internal static class PropertyFilterMapper
{
    public static DomainEnums.LandClassification? ToDomain(this ContractsEnums.LandClassification? value) => value switch
    {
        null => null,
        ContractsEnums.LandClassification.A => DomainEnums.LandClassification.A,
        ContractsEnums.LandClassification.B => DomainEnums.LandClassification.B,
        ContractsEnums.LandClassification.C => DomainEnums.LandClassification.C,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "قيمة تصنيف الأرض غير مدعومة.")
    };

    public static DomainEnums.LegalStatus? ToDomain(this ContractsEnums.LegalStatus? value) => value switch
    {
        null => null,
        ContractsEnums.LegalStatus.Tabo => DomainEnums.LegalStatus.Tabo,
        ContractsEnums.LegalStatus.Maliye => DomainEnums.LegalStatus.Maliye,
        ContractsEnums.LegalStatus.Taswiye => DomainEnums.LegalStatus.Taswiye,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "قيمة الوضع القانوني غير مدعومة.")
    };

    public static DomainEnums.PaymentType? ToDomain(this ContractsEnums.PaymentType? value) => value switch
    {
        null => null,
        ContractsEnums.PaymentType.Cash => DomainEnums.PaymentType.Cash,
        ContractsEnums.PaymentType.Installments => DomainEnums.PaymentType.Installments,
        ContractsEnums.PaymentType.DownPaymentAndInstallments => DomainEnums.PaymentType.DownPaymentAndInstallments,
        ContractsEnums.PaymentType.Negotiable => DomainEnums.PaymentType.Negotiable,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "طريقة الدفع غير مدعومة.")
    };

    public static DomainEnums.PropertyStatus? ToDomain(this ContractsEnums.PropertyStatus? value) => value switch
    {
        null => null,
        ContractsEnums.PropertyStatus.ForSale => DomainEnums.PropertyStatus.ForSale,
        ContractsEnums.PropertyStatus.ForRent => DomainEnums.PropertyStatus.ForRent,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "حالة العقار غير مدعومة.")
    };

    public static DomainEnums.PropertyType? ToDomain(this ContractsEnums.PropertyType? value) => value switch
    {
        null => null,
        ContractsEnums.PropertyType.Apartment => DomainEnums.PropertyType.Apartment,
        ContractsEnums.PropertyType.House => DomainEnums.PropertyType.House,
        ContractsEnums.PropertyType.Land => DomainEnums.PropertyType.Land,
        ContractsEnums.PropertyType.Office => DomainEnums.PropertyType.Office,
        ContractsEnums.PropertyType.Storage => DomainEnums.PropertyType.Storage,
        ContractsEnums.PropertyType.Building => DomainEnums.PropertyType.Building,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "نوع العقار غير مدعوم.")
    };
}
