using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyRemoveImageTests
{
    [Fact]
    public void RemoveImage_NonMainImage_RemovesIt()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 2);

        var result = property.RemoveImage(ids[1]);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(property.PropertyImages, i => i.Id == ids[1]);
    }

    [Fact]
    public void RemoveImage_UnknownImage_ReturnsImageNotFound()
    {
        var property = PropertyFactory.Create();
        PropertyFactory.AddImages(property, 2);

        var result = property.RemoveImage(Guid.NewGuid());

        Assert.Equal(PropertyErrors.ImageNotFound.Code, result.TopError.Code);
    }

    [Fact]
    public void RemoveImage_PropertyWithoutImages_ReturnsImageNotFound()
    {
        var result = PropertyFactory.Create().RemoveImage(Guid.NewGuid());

        Assert.Equal(PropertyErrors.ImageNotFound.Code, result.TopError.Code);
    }

    [Fact]
    public void RemoveImage_MainImage_ReturnsCannotRemoveMainImage()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 2);

        var result = property.RemoveImage(ids[0]);

        Assert.Equal(PropertyErrors.CannotRemoveMainImage.Code, result.TopError.Code);
        Assert.Equal(2, property.PropertyImages.Count);
    }

    [Fact]
    public void RemoveImage_ApprovedWithMinImages_ReturnsMinImagesRequired()
    {
        var property = PropertyFactory.CreateApproved(imageCount: Property.MinImages);
        var nonMain = property.PropertyImages.First(i => !i.IsMainImage);

        var result = property.RemoveImage(nonMain.Id);

        Assert.Equal(PropertyErrors.MinImagesRequired.Code, result.TopError.Code);
        Assert.Equal(Property.MinImages, property.PropertyImages.Count);
    }

    [Fact]
    public void RemoveImage_ApprovedAboveMinImages_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateApproved(imageCount: Property.MinImages + 1);
        var nonMain = property.PropertyImages.First(i => !i.IsMainImage);

        var result = property.RemoveImage(nonMain.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(Property.MinImages, property.PropertyImages.Count);
    }

    [Fact]
    public void RemoveImage_Approved_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved(imageCount: Property.MinImages + 1);
        var nonMain = property.PropertyImages.First(i => !i.IsMainImage);

        property.RemoveImage(nonMain.Id);

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultReviewerId, property.ReviewedBy);
    }

    [Fact]
    public void RemoveImage_PendingBelowMinImages_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateReadyForReview(imageCount: Property.MinImages);
        var nonMain = property.PropertyImages.First(i => !i.IsMainImage);

        var result = property.RemoveImage(nonMain.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(Property.MinImages - 1, property.PropertyImages.Count);
    }

    [Fact]
    public void RemoveImage_RejectedBelowMinImages_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateRejected();
        var nonMain = property.PropertyImages.First(i => !i.IsMainImage);

        var result = property.RemoveImage(nonMain.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RemoveImage_Rejected_StaysRejected()
    {
        var property = PropertyFactory.CreateRejected();
        var nonMain = property.PropertyImages.First(i => !i.IsMainImage);

        property.RemoveImage(nonMain.Id);

        Assert.Equal(ModerationStatus.Rejected, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultRejectionReason, property.RejectionReason);
    }

    [Fact]
    public void RemoveImage_NonMainImage_KeepsMainImage()
    {
        var property = PropertyFactory.Create();
        var ids = PropertyFactory.AddImages(property, 3);

        property.RemoveImage(ids[2]);

        Assert.Equal(ids[0], property.MainImage?.Id);
    }
}
