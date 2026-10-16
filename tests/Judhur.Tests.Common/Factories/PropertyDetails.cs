using Judhur.Domain.Properties.Enums;

namespace Judhur.Tests.Common.Factories;

public sealed record PropertyDetails
{
    public static PropertyDetails Valid { get; } = new();

    public string Title { get; init; } = "شقة مميزة في رام الله";

    public string? Description { get; init; } = "شقة واسعة في الطابق الثالث مع إطلالة جميلة";

    public decimal Price { get; init; } = 250_000m;

    public PaymentType PaymentType { get; init; } = PaymentType.Cash;

    public PropertyType PropertyType { get; init; } = PropertyType.Apartment;

    public PropertyStatus PropertyStatus { get; init; } = PropertyStatus.ForSale;

    public decimal Area { get; init; } = 150m;

    public string City { get; init; } = "رام الله";

    public string? Region { get; init; } = "الماصيون";

    public string FullAddress { get; init; } = "رام الله، الماصيون، شارع الإرسال";

    public double Latitude { get; init; } = 31.9038;

    public double Longitude { get; init; } = 35.2034;

    public LandClassification LandClassification { get; init; } = LandClassification.A;

    public LegalStatus LegalStatus { get; init; } = LegalStatus.Tabo;
}
