using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyMarketStatusTests
{
    private static readonly PropertyDetails ForRent = PropertyDetails.Valid with { PropertyStatus = PropertyStatus.ForRent };

    [Fact]
    public void MarkAsSold_ApprovedForSale_SetsSold()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.MarkAsSold();

        Assert.True(result.IsSuccess);
        Assert.Equal(PropertyStatus.Sold, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsSold_ApprovedForSale_StaysApprovedAndActive()
    {
        var property = PropertyFactory.CreateApproved();

        property.MarkAsSold();

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultReviewerId, property.ReviewedBy);
        Assert.True(property.IsActive);
    }

    [Fact]
    public void MarkAsSold_Pending_ReturnsCannotChangeStatusOfUnapprovedProperty()
    {
        var property = PropertyFactory.CreateReadyForReview();

        var result = property.MarkAsSold();

        Assert.Equal(PropertyErrors.CannotChangeStatusOfUnapprovedProperty.Code, result.TopError.Code);
        Assert.Equal(PropertyStatus.ForSale, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsSold_Rejected_ReturnsCannotChangeStatusOfUnapprovedProperty()
    {
        var property = PropertyFactory.CreateRejected();

        var result = property.MarkAsSold();

        Assert.Equal(PropertyErrors.CannotChangeStatusOfUnapprovedProperty.Code, result.TopError.Code);
        Assert.Equal(PropertyStatus.ForSale, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsSold_PendingForRent_ReturnsCannotChangeStatusOfUnapprovedPropertyFirst()
    {
        var property = PropertyFactory.CreateReadyForReview(ForRent);

        var result = property.MarkAsSold();

        Assert.Equal(PropertyErrors.CannotChangeStatusOfUnapprovedProperty.Code, result.TopError.Code);
    }

    [Fact]
    public void MarkAsSold_ApprovedForRent_ReturnsPropertyStatusTransitionInvalid()
    {
        var property = PropertyFactory.CreateApproved(ForRent);

        var result = property.MarkAsSold();

        Assert.Equal(PropertyErrors.PropertyStatusTransitionInvalid.Code, result.TopError.Code);
        Assert.Equal(PropertyStatus.ForRent, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsSold_AlreadySold_ReturnsPropertyStatusTransitionInvalid()
    {
        var property = PropertyFactory.CreateApproved();
        property.MarkAsSold().ThrowIfError();

        var result = property.MarkAsSold();

        Assert.Equal(PropertyErrors.PropertyStatusTransitionInvalid.Code, result.TopError.Code);
        Assert.Equal(PropertyStatus.Sold, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsSold_ApprovedInactive_SetsSold()
    {
        var property = PropertyFactory.CreateApproved();
        property.Deactivate().ThrowIfError();

        var result = property.MarkAsSold();

        Assert.True(result.IsSuccess);
        Assert.Equal(PropertyStatus.Sold, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsRented_ApprovedForRent_SetsRented()
    {
        var property = PropertyFactory.CreateApproved(ForRent);

        var result = property.MarkAsRented();

        Assert.True(result.IsSuccess);
        Assert.Equal(PropertyStatus.Rented, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsRented_ApprovedForRent_StaysApprovedAndActive()
    {
        var property = PropertyFactory.CreateApproved(ForRent);

        property.MarkAsRented();

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.True(property.IsActive);
    }

    [Fact]
    public void MarkAsRented_Pending_ReturnsCannotChangeStatusOfUnapprovedProperty()
    {
        var property = PropertyFactory.CreateReadyForReview(ForRent);

        var result = property.MarkAsRented();

        Assert.Equal(PropertyErrors.CannotChangeStatusOfUnapprovedProperty.Code, result.TopError.Code);
        Assert.Equal(PropertyStatus.ForRent, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsRented_Rejected_ReturnsCannotChangeStatusOfUnapprovedProperty()
    {
        var property = PropertyFactory.CreateRejected(ForRent);

        var result = property.MarkAsRented();

        Assert.Equal(PropertyErrors.CannotChangeStatusOfUnapprovedProperty.Code, result.TopError.Code);
    }

    [Fact]
    public void MarkAsRented_ApprovedForSale_ReturnsPropertyStatusTransitionInvalid()
    {
        var property = PropertyFactory.CreateApproved();

        var result = property.MarkAsRented();

        Assert.Equal(PropertyErrors.PropertyStatusTransitionInvalid.Code, result.TopError.Code);
        Assert.Equal(PropertyStatus.ForSale, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsRented_AlreadyRented_ReturnsPropertyStatusTransitionInvalid()
    {
        var property = PropertyFactory.CreateApproved(ForRent);
        property.MarkAsRented().ThrowIfError();

        var result = property.MarkAsRented();

        Assert.Equal(PropertyErrors.PropertyStatusTransitionInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void MarkAsRented_Sold_ReturnsPropertyStatusTransitionInvalid()
    {
        var property = PropertyFactory.CreateApproved();
        property.MarkAsSold().ThrowIfError();

        var result = property.MarkAsRented();

        Assert.Equal(PropertyErrors.PropertyStatusTransitionInvalid.Code, result.TopError.Code);
        Assert.Equal(PropertyStatus.Sold, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsSold_Rented_ReturnsPropertyStatusTransitionInvalid()
    {
        var property = PropertyFactory.CreateApproved(ForRent);
        property.MarkAsRented().ThrowIfError();

        var result = property.MarkAsSold();

        Assert.Equal(PropertyErrors.PropertyStatusTransitionInvalid.Code, result.TopError.Code);
        Assert.Equal(PropertyStatus.Rented, property.PropertyStatus);
    }

    [Fact]
    public void MarkAsSold_SoldThenEdited_KeepsSoldWhileReturningToPending()
    {
        var property = PropertyFactory.CreateApproved();
        property.MarkAsSold().ThrowIfError();

        PropertyFactory.UpdateDetails(property).ThrowIfError();

        Assert.Equal(PropertyStatus.Sold, property.PropertyStatus);
        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
    }
}
