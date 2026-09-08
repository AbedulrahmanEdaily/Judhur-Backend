using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;
using Judhur.Domain.Properties.PropertyImages;

namespace Judhur.Domain.Properties;

public sealed class Property : AuditableEntity
{
    private readonly List<PropertyImage> _propertyImages = [];

    private Property()
    { }

    private Property(
        Guid id,
        string title,
        string? description,
        decimal price,
        PaymentType paymentType,
        PropertyType propertyType,
        PropertyStatus propertyStatus,
        double area,
        string city,
        string? region,
        string fullAddress,
        double latitude,
        double longitude,
        LandClassification landClassification,
        LegalStatus legalStatus,
        string ownershipDocumentUrl,
        Guid sellerId,
        List<PropertyImage> propertyImages)
        : base(id)
    {
        Title = title;
        Description = description;
        Price = price;
        PaymentType = paymentType;
        PropertyType = propertyType;
        PropertyStatus = propertyStatus;
        Area = area;
        City = city;
        Region = region;
        FullAddress = fullAddress;
        Latitude = latitude;
        Longitude = longitude;
        LandClassification = landClassification;
        LegalStatus = legalStatus;
        OwnershipDocumentUrl = ownershipDocumentUrl;
        SellerId = sellerId;
        IsApproved = false;
        IsActive = true;
        _propertyImages = propertyImages;
    }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public PaymentType PaymentType { get; private set; }

    public PropertyType PropertyType { get; private set; }

    public PropertyStatus PropertyStatus { get; private set; }

    public double Area { get; private set; }

    public string City { get; private set; } = null!;

    public string? Region { get; private set; }

    public string FullAddress { get; private set; } = null!;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public LandClassification LandClassification { get; private set; }

    public LegalStatus LegalStatus { get; private set; }

    public string OwnershipDocumentUrl { get; private set; } = null!;

    public Guid SellerId { get; private set; }

    public bool IsApproved { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? ApprovedBy { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    public IEnumerable<PropertyImage> PropertyImages => _propertyImages.AsReadOnly();

    public PropertyImage? MainImage => _propertyImages.FirstOrDefault(i => i.IsMainImage);

    public static Result<Property> Create(
        Guid id,
        string title,
        string? description,
        decimal price,
        PaymentType paymentType,
        PropertyType propertyType,
        PropertyStatus propertyStatus,
        double area,
        string city,
        string? region,
        string fullAddress,
        double latitude,
        double longitude,
        LandClassification landClassification,
        LegalStatus legalStatus,
        string ownershipDocumentUrl,
        Guid sellerId,
        List<PropertyImage> propertyImages)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return PropertyErrors.TitleRequired;
        }

        if (price <= 0)
        {
            return PropertyErrors.PriceInvalid;
        }

        if (!Enum.IsDefined(paymentType))
        {
            return PropertyErrors.PaymentInvalid;
        }

        if (!Enum.IsDefined(propertyStatus))
        {
            return PropertyErrors.PropertyStatusInvalid;
        }

        if (!Enum.IsDefined(propertyType))
        {
            return PropertyErrors.PropertyTypeInvalid;
        }

        if (area <= 0)
        {
            return PropertyErrors.AreaInvalid;
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return PropertyErrors.CityRequired;
        }

        if (string.IsNullOrWhiteSpace(fullAddress))
        {
            return PropertyErrors.FullAddressRequired;
        }

        if (latitude < -90 || latitude > 90)
        {
            return PropertyErrors.LatitudeInvalid;
        }

        if (longitude < -180 || longitude > 180)
        {
            return PropertyErrors.LongitudeInvalid;
        }

        if (!Enum.IsDefined(landClassification))
        {
            return PropertyErrors.LandClassificationInvalid;
        }

        if (!Enum.IsDefined(legalStatus))
        {
            return PropertyErrors.LegalStatusInvalid;
        }

        if (string.IsNullOrWhiteSpace(ownershipDocumentUrl))
        {
            return PropertyErrors.OwnershipDocumentRequired;
        }

        if (sellerId == Guid.Empty)
        {
            return PropertyErrors.SellerRequired;
        }

        if (propertyImages is null || propertyImages.Count == 0)
        {
            return PropertyErrors.MainImageRequired;
        }

        if (!propertyImages.Any(i => i.IsMainImage))
        {
            return PropertyErrors.MainImageRequired;
        }

        return new Property(
            id,
            title,
            description,
            price,
            paymentType,
            propertyType,
            propertyStatus,
            area,
            city,
            region,
            fullAddress,
            latitude,
            longitude,
            landClassification,
            legalStatus,
            ownershipDocumentUrl,
            sellerId,
            propertyImages);
    }
}
