using Judhur.Domain.Properties.Enums;

namespace Judhur.Api.IntegrationTests.Marketplace;

public sealed record ListingBody
{
    public string Title { get; init; } = Market.NewTitle();

    public string? Description { get; init; } = "شقة واسعة قريبة من الخدمات";

    public decimal Price { get; init; } = 120_000m;

    public PaymentType PaymentType { get; init; } = PaymentType.Cash;

    public PropertyType PropertyType { get; init; } = PropertyType.Apartment;

    public PropertyStatus PropertyStatus { get; init; } = PropertyStatus.ForSale;

    public decimal Area { get; init; } = 140m;

    public string City { get; init; } = "نابلس";

    public string? Region { get; init; } = "رفيديا";

    public string FullAddress { get; init; } = "نابلس - رفيديا - شارع الجامعة";

    public double Latitude { get; init; } = 32.2211;

    public double Longitude { get; init; } = 35.2544;

    public LandClassification LandClassification { get; init; } = LandClassification.A;

    public LegalStatus LegalStatus { get; init; } = LegalStatus.Tabo;

    public object Details() => new
    {
        Title,
        Price,
        PaymentType,
        PropertyType,
        Area,
        City,
        Region,
        FullAddress,
        Latitude,
        Longitude,
        LandClassification,
        LegalStatus
    };
}
