using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;

namespace Judhur.Tests.Common.Factories;

public static class PropertyFactory
{
    public const string DefaultOwnershipDocumentPublicId = "judhur/documents/ownership-document";
    public const string DefaultRejectionReason = "الصور غير واضحة";

    public static readonly Guid DefaultSellerId = Guid.Parse("0199a1b2-0000-7000-8000-000000000001");
    public static readonly Guid DefaultReviewerId = Guid.Parse("0199a1b2-0000-7000-8000-0000000000a1");

    public static Result<Property> CreateResult(PropertyDetails? details = null, Guid? sellerId = null, Guid? id = null)
    {
        var d = details ?? PropertyDetails.Valid;

        return Property.Create(
            id ?? Guid.NewGuid(),
            d.Title,
            d.Description,
            d.Price,
            d.PaymentType,
            d.PropertyType,
            d.PropertyStatus,
            d.Area,
            d.City,
            d.Region,
            d.FullAddress,
            d.Latitude,
            d.Longitude,
            d.LandClassification,
            d.LegalStatus,
            sellerId ?? DefaultSellerId);
    }

    public static Property Create(PropertyDetails? details = null, Guid? sellerId = null, Guid? id = null)
        => CreateResult(details, sellerId, id).GetValueOrThrow();

    public static Result<Updated> UpdateDetails(Property property, PropertyDetails? details = null)
    {
        var d = details ?? PropertyDetails.Valid;

        return property.UpdateDetails(
            d.Title,
            d.Description,
            d.Price,
            d.PaymentType,
            d.PropertyType,
            d.Area,
            d.City,
            d.Region,
            d.FullAddress,
            d.Latitude,
            d.Longitude,
            d.LandClassification,
            d.LegalStatus);
    }

    public static string ImageUrl(Guid imageId)
        => $"https://res.cloudinary.com/judhur/image/upload/properties/{imageId:N}.jpg";

    public static string ImagePublicId(Guid imageId)
        => $"judhur/properties/{imageId:N}";

    public static Guid AddImage(Property property, bool isMainImage = false, Guid? imageId = null)
    {
        var id = imageId ?? Guid.NewGuid();
        property.AddImage(id, ImageUrl(id), ImagePublicId(id), isMainImage).ThrowIfError();
        return id;
    }

    public static IReadOnlyList<Guid> AddImages(Property property, int count)
    {
        var ids = new List<Guid>(count);
        for (var i = 0; i < count; i++)
        {
            ids.Add(AddImage(property));
        }

        return ids;
    }

    public static Property CreateReadyForReview(
        PropertyDetails? details = null,
        Guid? sellerId = null,
        int imageCount = Property.MinImages)
    {
        var property = Create(details, sellerId);
        AddImages(property, imageCount);
        property.SetOwnershipDocument(DefaultOwnershipDocumentPublicId).ThrowIfError();
        return property;
    }

    public static Property CreateApproved(
        PropertyDetails? details = null,
        Guid? sellerId = null,
        int imageCount = Property.MinImages)
    {
        var property = CreateReadyForReview(details, sellerId, imageCount);
        property.Approve(DefaultReviewerId, TestClock.UtcNow).ThrowIfError();
        return property;
    }

    public static Property CreateRejected(
        PropertyDetails? details = null,
        Guid? sellerId = null,
        string reason = DefaultRejectionReason)
    {
        var property = CreateReadyForReview(details, sellerId);
        property.Reject(DefaultReviewerId, TestClock.UtcNow, reason).ThrowIfError();
        return property;
    }
}
