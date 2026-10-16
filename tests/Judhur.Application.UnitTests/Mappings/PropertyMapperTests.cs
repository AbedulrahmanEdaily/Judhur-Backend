using Judhur.Application.Features.Properties.Mapper;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common.Factories;

namespace Judhur.Application.UnitTests.Mappings;

public sealed class PropertyMapperTests
{
    [Fact]
    public void ToDto_Property_CopiesAllDetails()
    {
        var details = PropertyDetails.Valid with
        {
            PaymentType = PaymentType.Negotiable,
            PropertyType = PropertyType.Land,
            PropertyStatus = PropertyStatus.ForRent,
            LandClassification = LandClassification.C,
            LegalStatus = LegalStatus.Taswiye
        };
        var property = PropertyFactory.Create(details);

        var dto = property.ToDto();

        Assert.Equal(property.Id, dto.Id);
        Assert.Equal(details.Title, dto.Title);
        Assert.Equal(details.Description, dto.Description);
        Assert.Equal(details.Price, dto.Price);
        Assert.Equal(details.PaymentType, dto.PaymentType);
        Assert.Equal(details.PropertyType, dto.PropertyType);
        Assert.Equal(details.PropertyStatus, dto.PropertyStatus);
        Assert.Equal(details.Area, dto.Area);
        Assert.Equal(details.City, dto.City);
        Assert.Equal(details.Region, dto.Region);
        Assert.Equal(details.FullAddress, dto.FullAddress);
        Assert.Equal(details.Latitude, dto.Latitude);
        Assert.Equal(details.Longitude, dto.Longitude);
        Assert.Equal(details.LandClassification, dto.LandClassification);
        Assert.Equal(details.LegalStatus, dto.LegalStatus);
    }

    [Fact]
    public void ToDto_Property_LeavesSellerFieldsEmpty()
    {
        var dto = PropertyFactory.Create().ToDto();

        Assert.Null(dto.User);
        Assert.Null(dto.SellerRating);
    }

    [Fact]
    public void ToDto_NoImages_ReturnsEmptyImages()
    {
        Assert.Empty(PropertyFactory.Create().ToDto().Images);
    }

    [Fact]
    public void ToDto_Images_MapsUrlOrderAndMainFlag()
    {
        var property = PropertyFactory.Create();
        var mainId = PropertyFactory.AddImage(property);
        var secondId = PropertyFactory.AddImage(property);

        var images = property.ToDto().Images;

        Assert.Equal([mainId, secondId], images.Select(i => i.Id));
        Assert.Equal([1, 2], images.Select(i => i.DisplayOrder));
        Assert.Equal(PropertyFactory.ImageUrl(mainId), images[0].Url);
        Assert.True(images[0].IsMainImage);
        Assert.False(images[1].IsMainImage);
    }

    [Fact]
    public void ToDto_ImagesAfterRemoval_KeepsDisplayOrderAscending()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 3);
        property.RemoveImage(ids[1]);
        var fourth = PropertyFactory.AddImage(property);

        var images = property.ToDto().Images;

        Assert.Equal([ids[0], ids[2], fourth], images.Select(i => i.Id));
        Assert.Equal([1, 3, 4], images.Select(i => i.DisplayOrder));
    }

    [Fact]
    public void ToDto_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((Property)null!).ToDto());
    }

    [Fact]
    public void ToDtos_Properties_MapsEachInOrder()
    {
        var first = PropertyFactory.Create();
        var second = PropertyFactory.Create();

        var dtos = new[] { first, second }.ToDtos();

        Assert.Equal([first.Id, second.Id], dtos.Select(d => d.Id));
    }

    [Fact]
    public void ToDtos_Empty_ReturnsEmptyList()
    {
        Assert.Empty(Array.Empty<Property>().ToDtos());
    }
}
