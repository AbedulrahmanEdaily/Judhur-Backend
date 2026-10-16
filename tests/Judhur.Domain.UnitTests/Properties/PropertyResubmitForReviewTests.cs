using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyResubmitForReviewTests
{
    [Fact]
    public void ResubmitForReview_Rejected_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateRejected();

        var result = property.ResubmitForReview();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ResubmitForReview_Rejected_ReturnsToPendingAndClearsReviewData()
    {
        var property = PropertyFactory.CreateRejected();

        property.ResubmitForReview();

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.RejectionReason);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
    }

    [Fact]
    public void ResubmitForReview_Rejected_KeepsImagesAndDocument()
    {
        var property = PropertyFactory.CreateRejected();

        property.ResubmitForReview();

        Assert.Equal(Property.MinImages, property.PropertyImages.Count);
        Assert.Equal(PropertyFactory.DefaultOwnershipDocumentPublicId, property.OwnershipDocumentPublicId);
    }

    [Fact]
    public void ResubmitForReview_Pending_ReturnsCannotResubmitUnrejectedProperty()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.ResubmitForReview();

        Assert.Equal(PropertyErrors.CannotResubmitUnrejectedProperty.Code, result.TopError.Code);
    }

    [Fact]
    public void ResubmitForReview_Approved_ReturnsCannotResubmitUnrejectedProperty()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.ResubmitForReview();

        Assert.Equal(PropertyErrors.CannotResubmitUnrejectedProperty.Code, result.TopError.Code);
    }

    [Fact]
    public void ResubmitForReview_Approved_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved();

        property.ResubmitForReview();

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultReviewerId, property.ReviewedBy);
    }

    [Fact]
    public void ResubmitForReview_Twice_ReturnsCannotResubmitUnrejectedProperty()
    {
        var property = PropertyFactory.CreateRejected();
        property.ResubmitForReview();

        var result = property.ResubmitForReview();

        Assert.Equal(PropertyErrors.CannotResubmitUnrejectedProperty.Code, result.TopError.Code);
    }

    [Fact]
    public void ResubmitForReview_Resubmitted_CanBeRejectedAgain()
    {
        var property = PropertyFactory.CreateRejected();
        property.ResubmitForReview();

        var result = property.Reject(PropertyFactory.DefaultReviewerId, TestClock.UtcNow, "ما زالت الصور غير واضحة");

        Assert.True(result.IsSuccess);
        Assert.Equal("ما زالت الصور غير واضحة", property.RejectionReason);
    }
}
