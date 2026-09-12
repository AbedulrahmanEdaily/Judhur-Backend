using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;
using Judhur.Domain.Properties.PropertyImages;

namespace Judhur.Domain.Properties;

public sealed class Property : AuditableEntity
{
    public const int MinImages = 3;
    public const int MaxImages = 10;

    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;
    public const int MaxCityLength = 100;
    public const int MaxRegionLength = 100;
    public const int MaxFullAddressLength = 500;
    public const int MaxOwnershipDocumentUrlLength = 500;
    public const int MaxRejectionReasonLength = 500;
    
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
        decimal area,
        string city,
        string? region,
        string fullAddress,
        double latitude,
        double longitude,
        LandClassification landClassification,
        LegalStatus legalStatus,
        string ownershipDocumentUrl,
        Guid sellerId)
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
    }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public PaymentType PaymentType { get; private set; }

    public PropertyType PropertyType { get; private set; }

    public PropertyStatus PropertyStatus { get; private set; }

    public decimal Area { get; private set; }

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

    public IReadOnlyCollection<PropertyImage> PropertyImages => _propertyImages.AsReadOnly();

    public PropertyImage? MainImage => _propertyImages.FirstOrDefault(i => i.IsMainImage);

    public static Result<Property> Create(
        Guid id,
        string title,
        string? description,
        decimal price,
        PaymentType paymentType,
        PropertyType propertyType,
        PropertyStatus propertyStatus,
        decimal area,
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
        var detailsError = ValidateDetails(
            title,
            description,
            price,
            paymentType,
            propertyType,
            area,
            city,
            region,
            fullAddress,
            latitude,
            longitude,
            landClassification,
            legalStatus);

        if (detailsError is not null)
        {
            return detailsError.Value;
        }

        if (propertyStatus is not (PropertyStatus.ForSale or PropertyStatus.ForRent))
        {
            return PropertyErrors.InitialPropertyStatusInvalid;
        }

        if (string.IsNullOrWhiteSpace(ownershipDocumentUrl))
        {
            return PropertyErrors.OwnershipDocumentRequired;
        }

        if (ownershipDocumentUrl.Length > MaxOwnershipDocumentUrlLength)
        {
            return PropertyErrors.OwnershipDocumentUrlTooLong;
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
            sellerId);
    }

    public Result<Updated> UpdateDetails(
        string title,
        string? description,
        decimal price,
        PaymentType paymentType,
        PropertyType propertyType,
        decimal area,
        string city,
        string? region,
        string fullAddress,
        double latitude,
        double longitude,
        LandClassification landClassification,
        LegalStatus legalStatus)
    {
        var detailsError = ValidateDetails(
            title,
            description,
            price,
            paymentType,
            propertyType,
            area,
            city,
            region,
            fullAddress,
            latitude,
            longitude,
            landClassification,
            legalStatus);

        if (detailsError is not null)
        {
            return detailsError.Value;
        }

        Title = title;
        Description = description;
        Price = price;
        PaymentType = paymentType;
        PropertyType = propertyType;
        Area = area;
        City = city;
        Region = region;
        FullAddress = fullAddress;
        Latitude = latitude;
        Longitude = longitude;
        LandClassification = landClassification;
        LegalStatus = legalStatus;

        ResetModeration();

        return Result.Updated;
    }

    public Result<Updated> UpdateDescription(string? description)
    {
        if (description?.Length > MaxDescriptionLength)
        {
            return PropertyErrors.DescriptionTooLong;
        }

        Description = description;

        ResetModeration();

        return Result.Updated;
    }

    public Result<Updated> Approve(Guid reviewedBy, DateTimeOffset reviewedAtUtc)
    {
        if (reviewedBy == Guid.Empty)
        {
            return PropertyErrors.ReviewerRequired;
        }

        if (ModerationStatus == ModerationStatus.Approved)
        {
            return PropertyErrors.AlreadyApproved;
        }

        if (_propertyImages.Count < MinImages)
        {
            return PropertyErrors.MinImagesRequired;
        }

        if (MainImage is null)
        {
            return PropertyErrors.MainImageRequired;
        }

        ModerationStatus = ModerationStatus.Approved;
        RejectionReason = null;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = reviewedAtUtc;

        return Result.Updated;
    }

    public Result<Updated> Reject(Guid reviewedBy, DateTimeOffset reviewedAtUtc, string reason)
    {
        if (reviewedBy == Guid.Empty)
        {
            return PropertyErrors.ReviewerRequired;
        }

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

        if (reason.Length > MaxRejectionReasonLength)
        {
            return PropertyErrors.RejectionReasonTooLong;
        }

        ModerationStatus = ModerationStatus.Rejected;
        RejectionReason = reason;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = reviewedAtUtc;

        return Result.Updated;
    }

    public Result<Updated> MarkAsSold()
        => ChangeMarketStatus(PropertyStatus.ForSale, PropertyStatus.Sold);

    public Result<Updated> MarkAsRented()
        => ChangeMarketStatus(PropertyStatus.ForRent, PropertyStatus.Rented);

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

    public Result<Updated> AddImage(Guid imageId, string fileUrl, string publicId, bool isMainImage)
    {
        if (_propertyImages.Count >= MaxImages)
        {
            return PropertyErrors.MaxImagesReached;
        }

        if (_propertyImages.Any(i => i.Id == imageId))
        {
            return PropertyErrors.DuplicateImageId;
        }

        var shouldBeMainImage = isMainImage || _propertyImages.Count == 0;

        var displayOrder = _propertyImages.Count == 0
            ? 1
            : _propertyImages.Max(i => i.DisplayOrder) + 1;

        var imageResult = PropertyImage.Create(imageId, Id, fileUrl, publicId, displayOrder, shouldBeMainImage);

        if (imageResult.IsError)
        {
            return imageResult.Errors;
        }

        var image = imageResult.Value;

        if (shouldBeMainImage)
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

        if (ModerationStatus == ModerationStatus.Approved && _propertyImages.Count - 1 < MinImages)
        {
            return PropertyErrors.MinImagesRequired;
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

    private static Error? ValidateDetails(
        string title,
        string? description,
        decimal price,
        PaymentType paymentType,
        PropertyType propertyType,
        decimal area,
        string city,
        string? region,
        string fullAddress,
        double latitude,
        double longitude,
        LandClassification landClassification,
        LegalStatus legalStatus)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return PropertyErrors.TitleRequired;
        }

        if (title.Length > MaxTitleLength)
        {
            return PropertyErrors.TitleTooLong;
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return PropertyErrors.DescriptionTooLong;
        }

        if (price <= 0)
        {
            return PropertyErrors.PriceInvalid;
        }

        if (!Enum.IsDefined(paymentType))
        {
            return PropertyErrors.PaymentInvalid;
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

        if (city.Length > MaxCityLength)
        {
            return PropertyErrors.CityTooLong;
        }

        if (region?.Length > MaxRegionLength)
        {
            return PropertyErrors.RegionTooLong;
        }

        if (string.IsNullOrWhiteSpace(fullAddress))
        {
            return PropertyErrors.FullAddressRequired;
        }

        if (fullAddress.Length > MaxFullAddressLength)
        {
            return PropertyErrors.FullAddressTooLong;
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

        return null;
    }

    private Result<Updated> ChangeMarketStatus(PropertyStatus expectedCurrent, PropertyStatus next)
    {
        if (ModerationStatus != ModerationStatus.Approved)
        {
            return PropertyErrors.CannotChangeStatusOfUnapprovedProperty;
        }

        if (PropertyStatus != expectedCurrent)
        {
            return PropertyErrors.PropertyStatusTransitionInvalid;
        }

        PropertyStatus = next;

        return Result.Updated;
    }

    private void ResetModeration()
    {
        if (ModerationStatus == ModerationStatus.Pending)
        {
            return;
        }

        ModerationStatus = ModerationStatus.Pending;
        RejectionReason = null;
        ReviewedBy = null;
        ReviewedAtUtc = null;
    }
}
