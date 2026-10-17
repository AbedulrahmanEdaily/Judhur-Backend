using Judhur.Domain.Properties;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyAddImageTests
{
    [Fact]
    public void AddImage_ValidImage_ReturnsSuccess()
    {
        var property = PropertyFactory.Create();
        var imageId = Guid.NewGuid();

        var result = property.AddImage(imageId, PropertyFactory.ImageUrl(imageId), PropertyFactory.ImagePublicId(imageId), false);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddImage_ValidImage_StoresImageData()
    {
        var property = PropertyFactory.Create();
        var imageId = Guid.NewGuid();
        var url = PropertyFactory.ImageUrl(imageId);
        var publicId = PropertyFactory.ImagePublicId(imageId);

        property.AddImage(imageId, url, publicId, false);

        var image = Assert.Single(property.PropertyImages);
        Assert.Equal(imageId, image.Id);
        Assert.Equal(property.Id, image.PropertyId);
        Assert.Equal(url, image.FileUrl);
        Assert.Equal(publicId, image.PublicId);
    }

    [Fact]
    public void AddImage_FirstImageNotFlaggedMain_BecomesMainImage()
    {
        var property = PropertyFactory.Create();

        var imageId = PropertyFactory.AddImage(property, isMainImage: false);

        Assert.Equal(imageId, property.MainImage?.Id);
    }

    [Fact]
    public void AddImage_SecondImageNotFlaggedMain_KeepsExistingMainImage()
    {
        var property = PropertyFactory.Create();
        var firstId = PropertyFactory.AddImage(property);

        var secondId = PropertyFactory.AddImage(property, isMainImage: false);

        Assert.Equal(firstId, property.MainImage?.Id);
        Assert.False(property.PropertyImages.Single(i => i.Id == secondId).IsMainImage);
    }

    [Fact]
    public void AddImage_NewImageFlaggedMain_BecomesTheOnlyMainImage()
    {
        var property = PropertyFactory.Create();
        PropertyFactory.AddImages(property, 2);

        var newMainId = PropertyFactory.AddImage(property, isMainImage: true);

        var main = Assert.Single(property.PropertyImages, i => i.IsMainImage);
        Assert.Equal(newMainId, main.Id);
    }

    [Fact]
    public void AddImage_FirstImage_GetsDisplayOrderOne()
    {
        var property = PropertyFactory.Create();

        PropertyFactory.AddImage(property);

        Assert.Equal(1, Assert.Single(property.PropertyImages).DisplayOrder);
    }

    [Fact]
    public void AddImage_SeveralImages_GetsIncreasingDisplayOrder()
    {
        var property = PropertyFactory.Create();

        PropertyFactory.AddImages(property, 4);

        Assert.Equal([1, 2, 3, 4], property.PropertyImages.Select(i => i.DisplayOrder));
    }

    [Fact]
    public void AddImage_AfterRemovingAMiddleImage_ContinuesFromHighestDisplayOrder()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 3);
        property.RemoveImage(ids[1]);

        var newId = PropertyFactory.AddImage(property);

        Assert.Equal(4, property.PropertyImages.Single(i => i.Id == newId).DisplayOrder);
    }

    [Fact]
    public void AddImage_AfterRemovingTheLastImage_ReusesItsDisplayOrder()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 3);
        property.RemoveImage(ids[2]);

        var newId = PropertyFactory.AddImage(property);

        Assert.Equal(3, property.PropertyImages.Single(i => i.Id == newId).DisplayOrder);
    }

    [Fact]
    public void AddImage_UpToMaxImages_ReturnsSuccess()
    {
        var property = PropertyFactory.Create();
        PropertyFactory.AddImages(property, Property.MaxImages - 1);
        var imageId = Guid.NewGuid();

        var result = property.AddImage(imageId, PropertyFactory.ImageUrl(imageId), PropertyFactory.ImagePublicId(imageId), false);

        Assert.True(result.IsSuccess);
        Assert.Equal(Property.MaxImages, property.PropertyImages.Count);
    }

    [Fact]
    public void AddImage_AtMaxImages_ReturnsMaxImagesReached()
    {
        var property = PropertyFactory.Create();
        PropertyFactory.AddImages(property, Property.MaxImages);
        var imageId = Guid.NewGuid();

        var result = property.AddImage(imageId, PropertyFactory.ImageUrl(imageId), PropertyFactory.ImagePublicId(imageId), false);

        Assert.Equal(PropertyErrors.MaxImagesReached.Code, result.TopError.Code);
        Assert.Equal(Property.MaxImages, property.PropertyImages.Count);
    }

    [Fact]
    public void AddImage_AtMaxImagesWithDuplicateId_ReturnsMaxImagesReachedFirst()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, Property.MaxImages);

        var result = property.AddImage(ids[0], PropertyFactory.ImageUrl(ids[0]), PropertyFactory.ImagePublicId(ids[0]), false);

        Assert.Equal(PropertyErrors.MaxImagesReached.Code, result.TopError.Code);
    }

    [Fact]
    public void AddImage_DuplicateImageId_ReturnsDuplicateImageId()
    {
        var property = PropertyFactory.Create();
        var imageId = PropertyFactory.AddImage(property);

        var result = property.AddImage(imageId, PropertyFactory.ImageUrl(imageId), PropertyFactory.ImagePublicId(imageId), false);

        Assert.Equal(PropertyErrors.DuplicateImageId.Code, result.TopError.Code);
        Assert.Single(property.PropertyImages);
    }

    [Fact]
    public void AddImage_EmptyImageId_GeneratesImageId()
    {
        var property = PropertyFactory.Create();

        property.AddImage(Guid.Empty, PropertyFactory.ImageUrl(Guid.NewGuid()), "judhur/properties/generated", false);

        Assert.NotEqual(Guid.Empty, Assert.Single(property.PropertyImages).Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddImage_BlankFileUrl_ReturnsFileUrlRequired(string fileUrl)
    {
        var property = PropertyFactory.Create();

        var result = property.AddImage(Guid.NewGuid(), fileUrl, "judhur/properties/1", false);

        Assert.Equal(Domain.Properties.PropertyImages.PropertyImageErrors.FileUrlRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void AddImage_InvalidImage_DoesNotAddImage()
    {
        var property = PropertyFactory.Create();

        property.AddImage(Guid.NewGuid(), "", "judhur/properties/1", true);

        Assert.Empty(property.PropertyImages);
    }

    [Fact]
    public void AddImage_InvalidImageFlaggedMain_KeepsExistingMainImage()
    {
        var property = PropertyFactory.Create();
        var mainId = PropertyFactory.AddImage(property);

        property.AddImage(Guid.NewGuid(), "", "judhur/properties/1", true);

        Assert.Equal(mainId, property.MainImage?.Id);
    }
}
