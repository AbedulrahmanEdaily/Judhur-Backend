using Judhur.Domain.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Common;

public sealed class EntityTests
{
    private sealed class TestEntity(Guid id) : Entity(id);

    [Fact]
    public void Constructor_GivenId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var entity = new TestEntity(id);

        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void Constructor_EmptyId_GeneratesVersion7Id()
    {
        var entity = new TestEntity(Guid.Empty);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(7, entity.Id.Version);
    }

    [Fact]
    public void Constructor_EmptyIdTwice_GeneratesDifferentIds()
    {
        var first = new TestEntity(Guid.Empty);
        var second = new TestEntity(Guid.Empty);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void AuditableEntity_NewlyCreated_HasNoAuditData()
    {
        var favorite = FavoriteFactory.Create();

        Assert.Equal(default, favorite.CreatedAtUtc);
        Assert.Null(favorite.CreatedBy);
        Assert.Equal(default, favorite.LastModifiedUtc);
        Assert.Null(favorite.LastModifiedBy);
    }
}
