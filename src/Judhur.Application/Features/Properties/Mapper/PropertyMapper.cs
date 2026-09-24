using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Properties;

namespace Judhur.Application.Features.Properties.Mapper;

public static class PropertyMapper
{
    public static PropertyDto ToDto(this Property entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new PropertyDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Price = entity.Price,
            PaymentType = entity.PaymentType,
            PropertyType = entity.PropertyType,
            PropertyStatus = entity.PropertyStatus,
            Area = entity.Area,
            City = entity.City,
            Region = entity.Region,
            FullAddress = entity.FullAddress,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            LandClassification = entity.LandClassification,
            LegalStatus = entity.LegalStatus,
            OwnershipDocumentUrl = entity.OwnershipDocumentUrl
        };
    }
    public static PropertyDto ToDto(this Property entity, UserInfoDto userInfo)
    {
        var dto = entity.ToDto();
        dto.User = userInfo;
        return dto;
    }
    public static List<PropertyDto> ToDtos(this IEnumerable<Property> entities)
    {
        return [.. entities.Select(p => p.ToDto())];
    }
}