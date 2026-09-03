using System.Security.Cryptography.X509Certificates;

using Judhur.Domain.Common;
using Judhur.Domain.Common.Result;

namespace Judhur.Domain.Property.PropertyImages;

public class PropertyImage : Entity
{
    public Guid PropertyId { get; private set; }
    public string FileUrl { get; private set; }
    public string PublicId { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsMainImage { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private PropertyImage()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    private PropertyImage(
        Guid id,
        Guid propertyId,
        string fileUrl,
        string publicId,
        int displayOrder,
        bool isMainImage) : base(id)
    {
        PropertyId = propertyId;
        FileUrl = fileUrl;
        PublicId = publicId;
        DisplayOrder = displayOrder;
        IsMainImage = isMainImage;
    }
    public static Result<PropertyImage> Create(Guid id, Guid propertyId, string fileUrl, string publicId, int displayOrder, bool isMainImage)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return PropertyImageErrors.FileUrlRequired;

        if (string.IsNullOrWhiteSpace(publicId))
            return PropertyImageErrors.PublicIdRequired;

        if (displayOrder < 1)
            return PropertyImageErrors.DisplayOrderInvalid;

        if (propertyId == Guid.Empty)
            return PropertyImageErrors.PropertyIdRequired;

        return new PropertyImage(id, propertyId, fileUrl, publicId, displayOrder, isMainImage);
    }
}