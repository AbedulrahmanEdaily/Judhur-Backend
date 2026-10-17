using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyRejectTests
{
    private const string Reason = "وثيقة الملكية غير مقروءة";

    private static readonly Guid ReviewerId = PropertyFactory.DefaultReviewerId;
    private static readonly DateTimeOffset ReviewedAt = TestClock.UtcNow.AddHours(3);

    [Fact]
    public void Reject_Pending_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.Reject(ReviewerId, ReviewedAt, Reason);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Reject_Pending_SetsRejectedAndReviewData()
    {
        var property = PropertyFactory.CreateReadyForReview();

        property.Reject(ReviewerId, ReviewedAt, Reason);

        Assert.Equal(ModerationStatus.Rejected, property.ModerationStatus);
        Assert.Equal(Reason, property.RejectionReason);
        Assert.Equal(ReviewerId, property.ReviewedBy);
        Assert.Equal(ReviewedAt, property.ReviewedAtUtc);
    }

    [Fact]
    public void Reject_PendingWithoutImagesOrDocument_ReturnsSuccess()
    {
        var property = PropertyFactory.Create();

        var result = property.Reject(ReviewerId, ReviewedAt, Reason);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Reject_Pending_KeepsImagesDocumentAndActiveState()
    {
        var property = PropertyFactory.CreateReadyForReview();

        property.Reject(ReviewerId, ReviewedAt, Reason);

        Assert.Equal(Property.MinImages, property.PropertyImages.Count);
        Assert.Equal(PropertyFactory.DefaultOwnershipDocumentPublicId, property.OwnershipDocumentPublicId);
        Assert.True(property.IsActive);
    }

    [Fact]
    public void Reject_ReasonWithSurroundingWhitespace_StoresTrimmedReason()
    {
        var property = PropertyFactory.CreateReadyForReview();

        property.Reject(ReviewerId, ReviewedAt, $"  {Reason}\n");

        Assert.Equal(Reason, property.RejectionReason);
    }

    [Fact]
    public void Reject_ReasonAtLimit_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.Reject(ReviewerId, ReviewedAt, new string('س', Property.MaxRejectionReasonLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Reject_ReasonAtLimitAfterTrimming_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateReadyForReview();
        var reason = new string('س', Property.MaxRejectionReasonLength);

        var result = property.Reject(ReviewerId, ReviewedAt, $"   {reason}   ");

        Assert.True(result.IsSuccess);
        Assert.Equal(reason, property.RejectionReason);
    }

    [Fact]
    public void Reject_ReasonOverLimit_ReturnsRejectionReasonTooLong()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.Reject(ReviewerId, ReviewedAt, new string('س', Property.MaxRejectionReasonLength + 1));

        Assert.Equal(PropertyErrors.RejectionReasonTooLong.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Reject_BlankReason_ReturnsRejectionReasonRequired(string reason)
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.Reject(ReviewerId, ReviewedAt, reason);

        Assert.Equal(PropertyErrors.RejectionReasonRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Reject_NullReason_ReturnsRejectionReasonRequired()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.Reject(ReviewerId, ReviewedAt, null!);

        Assert.Equal(PropertyErrors.RejectionReasonRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Reject_EmptyReviewer_ReturnsReviewerRequired()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.Reject(Guid.Empty, ReviewedAt, Reason);

        Assert.Equal(PropertyErrors.ReviewerRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Reject_EmptyReviewerAndBlankReason_ReturnsReviewerRequiredFirst()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.Reject(Guid.Empty, ReviewedAt, "");

        Assert.Equal(PropertyErrors.ReviewerRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Reject_Approved_ReturnsCannotRejectApprovedProperty()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.Reject(ReviewerId, ReviewedAt, Reason);

        Assert.Equal(PropertyErrors.CannotRejectApprovedProperty.Code, result.TopError.Code);
    }

    [Fact]
    public void Reject_Approved_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved();

        property.Reject(ReviewerId, ReviewedAt, Reason);

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Null(property.RejectionReason);
    }

    [Fact]
    public void Reject_AlreadyRejected_ReturnsAlreadyRejected()
    {
        var property = PropertyFactory.CreateRejected();

        var result = property.Reject(ReviewerId, ReviewedAt, Reason);

        Assert.Equal(PropertyErrors.AlreadyRejected.Code, result.TopError.Code);
    }

    [Fact]
    public void Reject_AlreadyRejected_KeepsOriginalReason()
    {
        var property = PropertyFactory.CreateRejected();

        property.Reject(Guid.NewGuid(), ReviewedAt, Reason);

        Assert.Equal(PropertyFactory.DefaultRejectionReason, property.RejectionReason);
        Assert.Equal(PropertyFactory.DefaultReviewerId, property.ReviewedBy);
        Assert.Equal(TestClock.UtcNow, property.ReviewedAtUtc);
    }

    [Fact]
    public void Reject_AlreadyRejectedWithBlankReason_ReturnsAlreadyRejectedFirst()
    {
        var property = PropertyFactory.CreateRejected();

        var result = property.Reject(ReviewerId, ReviewedAt, "");

        Assert.Equal(PropertyErrors.AlreadyRejected.Code, result.TopError.Code);
    }

    [Fact]
    public void Reject_InvalidReason_StaysPendingWithoutReviewData()
    {
        var property = PropertyFactory.CreateReadyForReview();

        property.Reject(ReviewerId, ReviewedAt, new string('س', Property.MaxRejectionReasonLength + 1));

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.Null(property.RejectionReason);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
    }
}
