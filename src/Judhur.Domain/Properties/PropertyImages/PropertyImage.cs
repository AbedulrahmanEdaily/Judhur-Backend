using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Properties.PropertyImages;

public sealed class PropertyImage : Entity
{
    public const int MaxFileUrlLength = 500;
    public const int MaxPublicIdLength = 200;

    private PropertyImage()
    { }

    private PropertyImage(
        Guid id,
        Guid propertyId,
        string fileUrl,
        string publicId,
        int displayOrder,
        bool isMainImage)
        : base(id)
    {
        PropertyId = propertyId;
        FileUrl = fileUrl;
        PublicId = publicId;
        DisplayOrder = displayOrder;
        IsMainImage = isMainImage;
    }

    public Guid PropertyId { get; private set; }

    public string FileUrl { get; private set; } = null!;

    public string PublicId { get; private set; } = null!;

    public int DisplayOrder { get; private set; }

    public bool IsMainImage { get; private set; }

    internal static Result<PropertyImage> Create(
        Guid id,
        Guid propertyId,
        string fileUrl,
        string publicId,
        int displayOrder,
        bool isMainImage)
    {
        if (propertyId == Guid.Empty)
        {
            return PropertyImageErrors.PropertyIdRequired;
        }

        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return PropertyImageErrors.FileUrlRequired;
        }

        if (fileUrl.Length > MaxFileUrlLength)
        {
            return PropertyImageErrors.FileUrlTooLong;
        }

        if (string.IsNullOrWhiteSpace(publicId))
        {
            return PropertyImageErrors.PublicIdRequired;
        }

        if (publicId.Length > MaxPublicIdLength)
        {
            return PropertyImageErrors.PublicIdTooLong;
        }

        if (displayOrder < 1)
        {
            return PropertyImageErrors.DisplayOrderInvalid;
        }

        return new PropertyImage(id, propertyId, fileUrl, publicId, displayOrder, isMainImage);
    }

    internal void SetAsMainImage(bool isMainImage)
    {
        IsMainImage = isMainImage;
    }
}
