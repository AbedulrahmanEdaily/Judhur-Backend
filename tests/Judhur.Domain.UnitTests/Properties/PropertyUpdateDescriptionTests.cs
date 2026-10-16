using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyUpdateDescriptionTests
{
    [Fact]
    public void UpdateDescription_ValidDescription_ReplacesDescription()
    {
        var property = PropertyFactory.Create();

        var result = property.UpdateDescription("وصف جديد");

        Assert.True(result.IsSuccess);
        Assert.Equal("وصف جديد", property.Description);
    }

    [Fact]
    public void UpdateDescription_Null_ClearsDescription()
    {
        var property = PropertyFactory.Create();

        var result = property.UpdateDescription(null);

        Assert.True(result.IsSuccess);
        Assert.Null(property.Description);
    }

    [Fact]
    public void UpdateDescription_AtLimit_ReturnsSuccess()
    {
        var property = PropertyFactory.Create();

        var result = property.UpdateDescription(new string('ع', Property.MaxDescriptionLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void UpdateDescription_OverLimit_ReturnsDescriptionTooLong()
    {
        var property = PropertyFactory.Create();

        var result = property.UpdateDescription(new string('ع', Property.MaxDescriptionLength + 1));

        Assert.Equal(PropertyErrors.DescriptionTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDescription_OverLimit_KeepsOldDescription()
    {
        var property = PropertyFactory.Create();
        var original = property.Description;

        property.UpdateDescription(new string('ع', Property.MaxDescriptionLength + 1));

        Assert.Equal(original, property.Description);
    }

    [Fact]
    public void UpdateDescription_ApprovedProperty_StaysApproved()
    {
        var property = PropertyFactory.CreateApproved();

        property.UpdateDescription("وصف معدل");

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultReviewerId, property.ReviewedBy);
    }

    [Fact]
    public void UpdateDescription_RejectedProperty_StaysRejected()
    {
        var property = PropertyFactory.CreateRejected();

        property.UpdateDescription("وصف معدل");

        Assert.Equal(ModerationStatus.Rejected, property.ModerationStatus);
        Assert.Equal(PropertyFactory.DefaultRejectionReason, property.RejectionReason);
    }
}
