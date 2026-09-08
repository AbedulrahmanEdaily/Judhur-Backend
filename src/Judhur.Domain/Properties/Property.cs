using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;
using Judhur.Domain.Properties.PropertyImages;

namespace Judhur.Domain.Properties;

public sealed class Property : AuditableEntity
{
    public const int MinImages = 3;
    public const int MaxImages = 10;

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
        ModerationStatus = ModerationStatus.Pending;
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

    public ModerationStatus ModerationStatus { get; private set; }

    public string? RejectionReason { get; private set; }

    public Guid? ReviewedBy { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public bool IsActive { get; private set; }

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
        Guid sellerId)
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

        if (propertyStatus is not (PropertyStatus.ForSale or PropertyStatus.ForRent))
        {
            return PropertyErrors.InitialPropertyStatusInvalid;
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
            []);
    }

    public Result<Updated> Approve(Guid reviewedBy, DateTimeOffset reviewedAtUtc)
    {
        if (ModerationStatus == ModerationStatus.Approved)
        {
            return PropertyErrors.AlreadyApproved;
        }

        if (_propertyImages.Count < MinImages)
        {
            return PropertyErrors.MinImagesRequired;
        }

        ModerationStatus = ModerationStatus.Approved;
        RejectionReason = null;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = reviewedAtUtc;

        return Result.Updated;
    }

    public Result<Updated> Reject(Guid reviewedBy, DateTimeOffset reviewedAtUtc, string reason)
    {
        if (ModerationStatus == ModerationStatus.Approved)
        {
            return PropertyErrors.CannotRejectApprovedProperty;
        }

        if (ModerationStatus == ModerationStatus.Rejected)
        {
            return PropertyErrors.AlreadyRejected;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return PropertyErrors.RejectionReasonRequired;
        }

        ModerationStatus = ModerationStatus.Rejected;
        RejectionReason = reason;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = reviewedAtUtc;

        return Result.Updated;
    }

    public Result<Updated> Deactivate()
    {
        if (!IsActive)
        {
            return PropertyErrors.AlreadyDeactivated;
        }

        IsActive = false;

        return Result.Updated;
    }

    public Result<Updated> Reactivate()
    {
        if (ModerationStatus != ModerationStatus.Approved)
        {
            return PropertyErrors.CannotReactivateUnapprovedProperty;
        }

        if (IsActive)
        {
            return PropertyErrors.AlreadyActive;
        }

        IsActive = true;

        return Result.Updated;
    }

    public Result<Updated> AddImage(string fileUrl, string publicId, bool isMainImage)
    {
        if (_propertyImages.Count >= MaxImages)
        {
            return PropertyErrors.MaxImagesReached;
        }

        var displayOrder = _propertyImages.Count + 1;

        var imageResult = PropertyImage.Create(Guid.NewGuid(), Id, fileUrl, publicId, displayOrder, isMainImage);

        if (imageResult.IsError)
        {
            return imageResult.Errors;
        }

        var image = imageResult.Value;

        if (isMainImage)
        {
            foreach (var existing in _propertyImages)
            {
                existing.SetAsMainImage(false);
            }
        }

        _propertyImages.Add(image);

        return Result.Updated;
    }

    public Result<Updated> RemoveImage(Guid imageId)
    {
        var image = _propertyImages.FirstOrDefault(i => i.Id == imageId);

        if (image is null)
        {
            return PropertyErrors.ImageNotFound;
        }

        if (image.IsMainImage)
        {
            return PropertyErrors.CannotRemoveMainImage;
        }

        _propertyImages.Remove(image);

        return Result.Updated;
    }

    public Result<Updated> SetMainImage(Guid imageId)
    {
        var image = _propertyImages.FirstOrDefault(i => i.Id == imageId);

        if (image is null)
        {
            return PropertyErrors.ImageNotFound;
        }

        foreach (var existing in _propertyImages)
        {
            existing.SetAsMainImage(existing.Id == imageId);
        }

        return Result.Updated;
    }
}
