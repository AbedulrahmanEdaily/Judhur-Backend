using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertySuspendByAdminTests
{
    private const string Reason = "الإعلان مخالف لسياسة النشر";

    private static readonly Guid AdminId = Guid.Parse("0199a1b2-0000-7000-8000-0000000000a9");
    private static readonly DateTimeOffset SuspendedAt = TestClock.UtcNow.AddDays(2);

    [Fact]
    public void SuspendByAdmin_Approved_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, Reason);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SuspendByAdmin_Approved_SetsRejectedWithAdminReviewData()
    {
        var property = PropertyFactory.CreateApproved();

        property.SuspendByAdmin(AdminId, SuspendedAt, Reason);

        Assert.Equal(ModerationStatus.Rejected, property.ModerationStatus);
        Assert.Equal(Reason, property.RejectionReason);
        Assert.Equal(AdminId, property.ReviewedBy);
        Assert.Equal(SuspendedAt, property.ReviewedAtUtc);
    }

    [Fact]
    public void SuspendByAdmin_Approved_KeepsMarketStatusActiveStateAndImages()
    {
        var property = PropertyFactory.CreateApproved();
        property.MarkAsSold().ThrowIfError();

        property.SuspendByAdmin(AdminId, SuspendedAt, Reason);

        Assert.Equal(PropertyStatus.Sold, property.PropertyStatus);
        Assert.True(property.IsActive);
        Assert.Equal(Property.MinImages, property.PropertyImages.Count);
    }

    [Fact]
    public void SuspendByAdmin_ReasonWithSurroundingWhitespace_StoresTrimmedReason()
    {
        var property = PropertyFactory.CreateApproved();

        property.SuspendByAdmin(AdminId, SuspendedAt, $"\t{Reason}  ");

        Assert.Equal(Reason, property.RejectionReason);
    }

    [Fact]
    public void SuspendByAdmin_ReasonAtLimit_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, new string('س', Property.MaxRejectionReasonLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SuspendByAdmin_ReasonAtLimitAfterTrimming_ReturnsSuccess()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, $"  {new string('س', Property.MaxRejectionReasonLength)}  ");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SuspendByAdmin_ReasonOverLimit_ReturnsRejectionReasonTooLong()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, new string('س', Property.MaxRejectionReasonLength + 1));

        Assert.Equal(PropertyErrors.RejectionReasonTooLong.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SuspendByAdmin_BlankReason_ReturnsRejectionReasonRequired(string reason)
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, reason);

        Assert.Equal(PropertyErrors.RejectionReasonRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void SuspendByAdmin_NullReason_ReturnsRejectionReasonRequired()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, null!);

        Assert.Equal(PropertyErrors.RejectionReasonRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void SuspendByAdmin_EmptyAdmin_ReturnsReviewerRequired()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.SuspendByAdmin(Guid.Empty, SuspendedAt, Reason);

        Assert.Equal(PropertyErrors.ReviewerRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void SuspendByAdmin_EmptyAdminOnPending_ReturnsReviewerRequiredFirst()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.SuspendByAdmin(Guid.Empty, SuspendedAt, Reason);

        Assert.Equal(PropertyErrors.ReviewerRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void SuspendByAdmin_Pending_ReturnsCannotSuspendUnapprovedProperty()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, Reason);

        Assert.Equal(PropertyErrors.CannotSuspendUnapprovedProperty.Code, result.TopError.Code);
        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
    }

    [Fact]
    public void SuspendByAdmin_Rejected_ReturnsCannotSuspendUnapprovedProperty()
    {
        var property = PropertyFactory.CreateRejected();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, Reason);

        Assert.Equal(PropertyErrors.CannotSuspendUnapprovedProperty.Code, result.TopError.Code);
        Assert.Equal(PropertyFactory.DefaultRejectionReason, property.RejectionReason);
    }

    [Fact]
    public void SuspendByAdmin_PendingWithBlankReason_ReturnsCannotSuspendUnapprovedPropertyFirst()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, "");

        Assert.Equal(PropertyErrors.CannotSuspendUnapprovedProperty.Code, result.TopError.Code);
    }

    [Fact]
    public void SuspendByAdmin_InvalidReason_StaysApprovedWithOriginalReviewData()
    {
        var property = PropertyFactory.CreateApproved();

        property.SuspendByAdmin(AdminId, SuspendedAt, " ");

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Null(property.RejectionReason);
        Assert.Equal(PropertyFactory.DefaultReviewerId, property.ReviewedBy);
        Assert.Equal(TestClock.UtcNow, property.ReviewedAtUtc);
    }

    [Fact]
    public void SuspendByAdmin_ThenResubmit_ReturnsToPending()
    {
        var property = PropertyFactory.CreateApproved();
        property.SuspendByAdmin(AdminId, SuspendedAt, Reason).ThrowIfError();

        var result = property.ResubmitForReview();

        Assert.True(result.IsSuccess);
        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
    }

    [Fact]
    public void SuspendByAdmin_Suspended_ReturnsCannotSuspendUnapprovedProperty()
    {
        var property = PropertyFactory.CreateApproved();
        property.SuspendByAdmin(AdminId, SuspendedAt, Reason).ThrowIfError();

        var result = property.SuspendByAdmin(AdminId, SuspendedAt, Reason);

        Assert.Equal(PropertyErrors.CannotSuspendUnapprovedProperty.Code, result.TopError.Code);
    }
}
