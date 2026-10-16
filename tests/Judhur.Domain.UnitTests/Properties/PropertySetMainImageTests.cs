using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertySetMainImageTests
{
    [Fact]
    public void SetMainImage_OtherImage_BecomesMainImage()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 3);

        var result = property.SetMainImage(ids[2]);

        Assert.True(result.IsSuccess);
        Assert.Equal(ids[2], property.MainImage?.Id);
    }

    [Fact]
    public void SetMainImage_OtherImage_LeavesExactlyOneMainImage()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 3);

        property.SetMainImage(ids[1]);

        Assert.Single(property.PropertyImages, i => i.IsMainImage);
    }

    [Fact]
    public void SetMainImage_BackToOlderImage_LeavesExactlyOneMainImage()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 3);
        property.SetMainImage(ids[2]);

        property.SetMainImage(ids[0]);

        var main = Assert.Single(property.PropertyImages, i => i.IsMainImage);
        Assert.Equal(ids[0], main.Id);
    }

    [Fact]
    public void SetMainImage_UnknownImage_ReturnsImageNotFound()
    {
        var property = PropertyFactory.Create();
        PropertyFactory.AddImages(property, 2);

        var result = property.SetMainImage(Guid.NewGuid());

        Assert.Equal(PropertyErrors.ImageNotFound.Code, result.TopError.Code);
    }

    [Fact]
    public void SetMainImage_CurrentMainImage_ReturnsSuccess()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 2);

        var result = property.SetMainImage(ids[0]);

        Assert.True(result.IsSuccess);
        Assert.Equal(ids[0], property.MainImage?.Id);
    }

    [Fact]
    public void SetMainImage_CurrentMainImageOnApproved_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved();
        var mainId = property.MainImage!.Id;

        property.SetMainImage(mainId);

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
    }

    [Fact]
    public void SetMainImage_UnknownImageOnApproved_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved();

        property.SetMainImage(Guid.NewGuid());

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
    }

    [Fact]
    public void SetMainImage_DoesNotChangeDisplayOrder()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 3);

        property.SetMainImage(ids[2]);

        Assert.Equal(3, property.PropertyImages.Single(i => i.Id == ids[2]).DisplayOrder);
    }
}
