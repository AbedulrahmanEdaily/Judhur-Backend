using ContractsEnums = Judhur.Contracts.Common;
using DomainEnums = Judhur.Domain.Properties.Enums;

namespace Judhur.Api.Mapping;

internal static class PropertyFilterMapper
{
    public static List<string> ToCityFilter(this List<string>? cities) =>
        cities is null
            ? []
            : [.. cities
                .Where(city => !string.IsNullOrWhiteSpace(city))
                .Select(city => city.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)];

    public static List<DomainEnums.LandClassification> ToDomain(this List<ContractsEnums.LandClassification>? values) =>
        values is null ? [] : [.. values.Distinct().Select(Map)];

    public static List<DomainEnums.LegalStatus> ToDomain(this List<ContractsEnums.LegalStatus>? values) =>
        values is null ? [] : [.. values.Distinct().Select(Map)];

    public static List<DomainEnums.PaymentType> ToDomain(this List<ContractsEnums.PaymentType>? values) =>
        values is null ? [] : [.. values.Distinct().Select(Map)];

    public static List<DomainEnums.PropertyStatus> ToDomain(this List<ContractsEnums.PropertyStatus>? values) =>
        values is null ? [] : [.. values.Distinct().Select(Map)];

    public static List<DomainEnums.PropertyType> ToDomain(this List<ContractsEnums.PropertyType>? values) =>
        values is null ? [] : [.. values.Distinct().Select(Map)];

    private static DomainEnums.LandClassification Map(ContractsEnums.LandClassification value) => value switch
    {
        ContractsEnums.LandClassification.A => DomainEnums.LandClassification.A,
        ContractsEnums.LandClassification.B => DomainEnums.LandClassification.B,
        ContractsEnums.LandClassification.C => DomainEnums.LandClassification.C,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "قيمة تصنيف الأرض غير مدعومة.")
    };

    private static DomainEnums.LegalStatus Map(ContractsEnums.LegalStatus value) => value switch
    {
        ContractsEnums.LegalStatus.Tabo => DomainEnums.LegalStatus.Tabo,
        ContractsEnums.LegalStatus.Maliye => DomainEnums.LegalStatus.Maliye,
        ContractsEnums.LegalStatus.Taswiye => DomainEnums.LegalStatus.Taswiye,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "قيمة الوضع القانوني غير مدعومة.")
    };

    private static DomainEnums.PaymentType Map(ContractsEnums.PaymentType value) => value switch
    {
        ContractsEnums.PaymentType.Cash => DomainEnums.PaymentType.Cash,
        ContractsEnums.PaymentType.Installments => DomainEnums.PaymentType.Installments,
        ContractsEnums.PaymentType.DownPaymentAndInstallments => DomainEnums.PaymentType.DownPaymentAndInstallments,
        ContractsEnums.PaymentType.Negotiable => DomainEnums.PaymentType.Negotiable,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "طريقة الدفع غير مدعومة.")
    };

    private static DomainEnums.PropertyStatus Map(ContractsEnums.PropertyStatus value) => value switch
    {
        ContractsEnums.PropertyStatus.ForSale => DomainEnums.PropertyStatus.ForSale,
        ContractsEnums.PropertyStatus.ForRent => DomainEnums.PropertyStatus.ForRent,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "حالة العقار غير مدعومة.")
    };

    private static DomainEnums.PropertyType Map(ContractsEnums.PropertyType value) => value switch
    {
        ContractsEnums.PropertyType.Apartment => DomainEnums.PropertyType.Apartment,
        ContractsEnums.PropertyType.House => DomainEnums.PropertyType.House,
        ContractsEnums.PropertyType.Land => DomainEnums.PropertyType.Land,
        ContractsEnums.PropertyType.Office => DomainEnums.PropertyType.Office,
        ContractsEnums.PropertyType.Storage => DomainEnums.PropertyType.Storage,
        ContractsEnums.PropertyType.Building => DomainEnums.PropertyType.Building,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "نوع العقار غير مدعوم.")
    };
}
