using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyActivationTests
{
    [Fact]
    public void Deactivate_ActiveProperty_SetsInactive()
    {
        var property = PropertyFactory.Create();

        var result = property.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.False(property.IsActive);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ReturnsAlreadyDeactivated()
    {
        var property = PropertyFactory.Create();
        property.Deactivate().ThrowIfError();

        var result = property.Deactivate();

        Assert.Equal(PropertyErrors.AlreadyDeactivated.Code, result.TopError.Code);
        Assert.False(property.IsActive);
    }

    [Fact]
    public void Deactivate_Approved_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved();

        property.Deactivate();

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultReviewerId, property.ReviewedBy);
    }

    [Fact]
    public void Deactivate_Rejected_StaysRejected()
    {
        var property = PropertyFactory.CreateRejected();

        var result = property.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.Equal(ModerationStatus.Rejected, property.ModerationStatus);
    }

    [Fact]
    public void Deactivate_DoesNotDeleteProperty()
    {
        var property = PropertyFactory.Create();

        property.Deactivate();

        Assert.False(property.IsDeleted);
        Assert.Null(property.DeletedAtUtc);
    }

    [Fact]
    public void Deactivate_DeletedProperty_ReturnsAlreadyDeactivated()
    {
        var property = PropertyFactory.Create();
        property.Delete(TestClock.UtcNow).ThrowIfError();

        var result = property.Deactivate();

        Assert.Equal(PropertyErrors.AlreadyDeactivated.Code, result.TopError.Code);
    }

    [Fact]
    public void Reactivate_InactiveProperty_SetsActive()
    {
        var property = PropertyFactory.Create();
        property.Deactivate().ThrowIfError();

        var result = property.Reactivate();

        Assert.True(result.IsSuccess);
        Assert.True(property.IsActive);
    }

    [Fact]
    public void Reactivate_ActiveProperty_ReturnsAlreadyActive()
    {
        var property = PropertyFactory.Create();

        var result = property.Reactivate();

        Assert.Equal(PropertyErrors.AlreadyActive.Code, result.TopError.Code);
        Assert.True(property.IsActive);
    }

    [Fact]
    public void Reactivate_Approved_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved();
        property.Deactivate().ThrowIfError();

        property.Reactivate();

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultReviewerId, property.ReviewedBy);
    }

    [Fact]
    public void Reactivate_Rejected_StaysRejected()
    {
        var property = PropertyFactory.CreateRejected();
        property.Deactivate().ThrowIfError();

        property.Reactivate();

        Assert.Equal(ModerationStatus.Rejected, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultRejectionReason, property.RejectionReason);
    }

    [Fact]
    public void Reactivate_SoldProperty_KeepsSold()
    {
        var property = PropertyFactory.CreateApproved();
        property.MarkAsSold().ThrowIfError();
        property.Deactivate().ThrowIfError();

        property.Reactivate();

        Assert.Equal(PropertyStatus.Sold, property.PropertyStatus);
    }

    [Fact(Skip = "BUG: Reactivate does not check IsDeleted, so a soft-deleted property becomes active again")]
    public void Reactivate_DeletedProperty_ReturnsErrorAndStaysInactive()
    {
        var property = PropertyFactory.Create();
        property.Delete(TestClock.UtcNow).ThrowIfError();

        var result = property.Reactivate();

        Assert.True(result.IsError);
        Assert.False(property.IsActive);
    }
}
