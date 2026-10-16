using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyDeleteTests
{
    private static readonly DateTimeOffset DeletedAt = TestClock.UtcNow.AddDays(5);

    [Fact]
    public void Delete_ExistingProperty_ReturnsSuccess()
    {
        var property = PropertyFactory.Create();

        var result = property.Delete(DeletedAt);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Delete_ExistingProperty_MarksDeletedAndInactive()
    {
        var property = PropertyFactory.Create();

        property.Delete(DeletedAt);

        Assert.True(property.IsDeleted);
        Assert.Equal(DeletedAt, property.DeletedAtUtc);
        Assert.False(property.IsActive);
    }

    [Fact]
    public void Delete_InactiveProperty_ReturnsSuccess()
    {
        var property = PropertyFactory.Create();
        property.Deactivate().ThrowIfError();

        var result = property.Delete(DeletedAt);

        Assert.True(result.IsSuccess);
        Assert.True(property.IsDeleted);
    }

    [Fact]
    public void Delete_AlreadyDeleted_ReturnsNotFound()
    {
        var property = PropertyFactory.Create();
        property.Delete(DeletedAt).ThrowIfError();

        var result = property.Delete(DeletedAt.AddDays(1));

        Assert.Equal(PropertyErrors.NotFound.Code, result.TopError.Code);
    }

    [Fact]
    public void Delete_AlreadyDeleted_KeepsOriginalDeletionTime()
    {
        var property = PropertyFactory.Create();
        property.Delete(DeletedAt).ThrowIfError();

        property.Delete(DeletedAt.AddDays(1));

        Assert.Equal(DeletedAt, property.DeletedAtUtc);
    }

    [Fact]
    public void Delete_ApprovedProperty_KeepsModerationAndImages()
    {
        var property = PropertyFactory.CreateApproved();

        property.Delete(DeletedAt);

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
        Assert.Equal(Property.MinImages, property.PropertyImages.Count);
        Assert.Equal(PropertyFactory.DefaultOwnershipDocumentPublicId, property.OwnershipDocumentPublicId);
    }
}
