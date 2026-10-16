using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyApproveTests
{
    private static readonly Guid ReviewerId = PropertyFactory.DefaultReviewerId;
    private static readonly DateTimeOffset ReviewedAt = TestClock.UtcNow.AddHours(3);

    [Fact]
    public void Approve_ReadyForReview_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.Approve(ReviewerId, ReviewedAt);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Approve_ReadyForReview_SetsApprovedAndReviewData()
    {
        var property = PropertyFactory.CreateReadyForReview();

        property.Approve(ReviewerId, ReviewedAt);

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Equal(ReviewerId, property.ReviewedBy);
        Assert.Equal(ReviewedAt, property.ReviewedAtUtc);
        Assert.Null(property.RejectionReason);
    }

    [Fact]
    public void Approve_ReadyForReview_KeepsMarketStatusAndActiveState()
    {
        var property = PropertyFactory.CreateReadyForReview(PropertyDetails.Valid with { PropertyStatus = PropertyStatus.ForRent });

        property.Approve(ReviewerId, ReviewedAt);

        Assert.Equal(PropertyStatus.ForRent, property.PropertyStatus);
        Assert.True(property.IsActive);
    }

    [Fact]
    public void Approve_MaxImages_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateReadyForReview(imageCount: Property.MaxImages);

        var result = property.Approve(ReviewerId, ReviewedAt);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Approve_EmptyReviewer_ReturnsReviewerRequired()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.Approve(Guid.Empty, ReviewedAt);

        Assert.Equal(PropertyErrors.ReviewerRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Approve_EmptyReviewerOnApproved_ReturnsReviewerRequiredFirst()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.Approve(Guid.Empty, ReviewedAt);

        Assert.Equal(PropertyErrors.ReviewerRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Approve_AlreadyApproved_ReturnsAlreadyApproved()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.Approve(Guid.NewGuid(), ReviewedAt);

        Assert.Equal(PropertyErrors.AlreadyApproved.Code, result.TopError.Code);
    }

    [Fact]
    public void Approve_AlreadyApproved_KeepsOriginalReviewData()
    {
        var property = PropertyFactory.CreateApproved();

        property.Approve(Guid.NewGuid(), ReviewedAt);

        Assert.Equal(PropertyFactory.DefaultReviewerId, property.ReviewedBy);
        Assert.Equal(TestClock.UtcNow, property.ReviewedAtUtc);
    }

    [Fact]
    public void Approve_Rejected_ReturnsCannotApproveRejectedProperty()
    {
        var property = PropertyFactory.CreateRejected();

        var result = property.Approve(ReviewerId, ReviewedAt);

        Assert.Equal(PropertyErrors.CannotApproveRejectedProperty.Code, result.TopError.Code);
    }

    [Fact]
    public void Approve_Rejected_StaysRejected()
    {
        var property = PropertyFactory.CreateRejected();

        property.Approve(ReviewerId, ReviewedAt);

        Assert.Equal(ModerationStatus.Rejected, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultRejectionReason, property.RejectionReason);
    }

    [Fact]
    public void Approve_ResubmittedAfterRejection_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateRejected();
        property.ResubmitForReview().ThrowIfError();

        var result = property.Approve(ReviewerId, ReviewedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Null(property.RejectionReason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(Property.MinImages - 1)]
    public void Approve_FewerThanMinImages_ReturnsMinImagesRequired(int imageCount)
    {
        var property = PropertyFactory.Create();
        PropertyFactory.AddImages(property, imageCount);
        property.SetOwnershipDocument(PropertyFactory.DefaultOwnershipDocumentPublicId).ThrowIfError();

        var result = property.Approve(ReviewerId, ReviewedAt);

        Assert.Equal(PropertyErrors.MinImagesRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Approve_WithoutOwnershipDocument_ReturnsOwnershipDocumentRequired()
    {
        var property = PropertyFactory.Create();
        PropertyFactory.AddImages(property, Property.MinImages);

        var result = property.Approve(ReviewerId, ReviewedAt);

        Assert.Equal(PropertyErrors.OwnershipDocumentRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Approve_WithoutImagesOrDocument_ReturnsMinImagesRequiredFirst()
    {
        var property = PropertyFactory.Create();

        var result = property.Approve(ReviewerId, ReviewedAt);

        Assert.Equal(PropertyErrors.MinImagesRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Approve_Failure_StaysPendingWithoutReviewData()
    {
        var property = PropertyFactory.Create();

        property.Approve(ReviewerId, ReviewedAt);

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
    }

    [Fact]
    public void Approve_InactiveProperty_ReturnsSuccessAndStaysInactive()
    {
        var property = PropertyFactory.CreateReadyForReview();
        property.Deactivate().ThrowIfError();

        var result = property.Approve(ReviewerId, ReviewedAt);

        Assert.True(result.IsSuccess);
        Assert.False(property.IsActive);
    }
}
