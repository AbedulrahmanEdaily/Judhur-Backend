using Judhur.Domain.Properties.Enums;

namespace Judhur.Infrastructure.Data.Seed;

internal sealed record DemoListing(
    string Key,
    string Seller,
    string Title,
    string? Description,
    decimal Price,
    PaymentType PaymentType,
    PropertyType PropertyType,
    PropertyStatus PropertyStatus,
    decimal Area,
    string City,
    string? Region,
    string FullAddress,
    double Latitude,
    double Longitude,
    LandClassification LandClassification,
    LegalStatus LegalStatus,
    DemoGallery Gallery,
    int ImageCount,
    DemoOutcome Outcome,
    int DaysAgo,
    string? ModerationReason = null,
    bool HasOwnershipDocument = true);
