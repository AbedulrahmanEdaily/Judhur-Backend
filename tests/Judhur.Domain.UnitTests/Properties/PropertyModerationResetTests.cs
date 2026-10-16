using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyModerationResetTests
{
    private static readonly PropertyDetails EditedDetails = PropertyDetails.Valid with { Title = "شقة معدلة في البيرة" };

    [Fact]
    public void UpdateDetails_Approved_ReturnsToPendingAndClearsReviewData()
    {
        var property = PropertyFactory.CreateApproved();

        PropertyFactory.UpdateDetails(property, EditedDetails);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
        Assert.Null(property.RejectionReason);
    }

    [Fact]
    public void UpdateDetails_Rejected_ReturnsToPendingAndClearsReason()
    {
        var property = PropertyFactory.CreateRejected();

        PropertyFactory.UpdateDetails(property, EditedDetails);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.RejectionReason);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
    }

    [Fact]
    public void UpdateDetails_Pending_StaysPending()
    {
        var property = PropertyFactory.CreateReadyForReview();

        PropertyFactory.UpdateDetails(property, EditedDetails);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
    }

    [Fact]
    public void UpdateDetails_ApprovedThenEdited_CanBeApprovedAgain()
    {
        var property = PropertyFactory.CreateApproved();
        PropertyFactory.UpdateDetails(property, EditedDetails).ThrowIfError();
        var reviewedAt = TestClock.UtcNow.AddDays(1);

        var result = property.Approve(PropertyFactory.DefaultReviewerId, reviewedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(reviewedAt, property.ReviewedAtUtc);
    }

    [Fact]
    public void UpdateDetails_RejectedThenEdited_CanBeApprovedWithoutResubmitting()
    {
        var property = PropertyFactory.CreateRejected();
        PropertyFactory.UpdateDetails(property, EditedDetails).ThrowIfError();

        var result = property.Approve(PropertyFactory.DefaultReviewerId, TestClock.UtcNow);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddImage_Approved_ReturnsToPendingAndClearsReviewData()
    {
        var property = PropertyFactory.CreateApproved();

        PropertyFactory.AddImage(property);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
    }

    [Fact]
    public void AddImage_Rejected_ReturnsToPendingAndClearsReason()
    {
        var property = PropertyFactory.CreateRejected();

        PropertyFactory.AddImage(property);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.RejectionReason);
    }

    [Fact]
    public void AddImage_FailureOnApproved_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved(imageCount: Property.MaxImages);
        var imageId = Guid.NewGuid();

        var result = property.AddImage(imageId, PropertyFactory.ImageUrl(imageId), PropertyFactory.ImagePublicId(imageId), false);

        Assert.Equal(PropertyErrors.MaxImagesReached.Code, result.TopError.Code);
        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
    }

    [Fact]
    public void SetMainImage_OtherImageOnApproved_ReturnsToPendingAndClearsReviewData()
    {
        var property = PropertyFactory.CreateApproved();
        var other = property.PropertyImages.First(i => !i.IsMainImage);

        property.SetMainImage(other.Id);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
    }

    [Fact]
    public void SetMainImage_OtherImageOnRejected_ReturnsToPendingAndClearsReason()
    {
        var property = PropertyFactory.CreateRejected();
        var other = property.PropertyImages.First(i => !i.IsMainImage);

        property.SetMainImage(other.Id);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.RejectionReason);
    }

    [Fact]
    public void AddImage_ApprovedSoldAndInactive_KeepsActiveStateAndMarketStatus()
    {
        var property = PropertyFactory.CreateApproved();
        property.MarkAsSold().ThrowIfError();
        property.Deactivate().ThrowIfError();

        PropertyFactory.AddImage(property);

        Assert.False(property.IsActive);
        Assert.Equal(PropertyStatus.Sold, property.PropertyStatus);
    }
}
