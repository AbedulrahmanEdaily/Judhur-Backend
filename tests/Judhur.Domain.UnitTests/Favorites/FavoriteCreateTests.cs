using Judhur.Domain.Favorites;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Favorites;

public sealed class FavoriteCreateTests
{
    [Fact]
    public void Create_ValidFavorite_ReturnsSuccess()
    {
        var result = FavoriteFactory.CreateResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ValidFavorite_CopiesUserAndProperty()
    {
        var userId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();

        var favorite = FavoriteFactory.Create(userId, propertyId);

        Assert.Equal(userId, favorite.UserId);
        Assert.Equal(propertyId, favorite.PropertyId);
    }

    [Fact]
    public void Create_GivenId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var favorite = FavoriteFactory.Create(id: id);

        Assert.Equal(id, favorite.Id);
    }

    [Fact]
    public void Create_EmptyId_GeneratesNewId()
    {
        var favorite = FavoriteFactory.Create(id: Guid.Empty);

        Assert.NotEqual(Guid.Empty, favorite.Id);
    }

    [Fact]
    public void Create_EmptyUser_ReturnsUserIdRequired()
    {
        var result = FavoriteFactory.CreateResult(userId: Guid.Empty);

        Assert.Equal(FavoriteErrors.UserIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptyProperty_ReturnsPropertyIdRequired()
    {
        var result = FavoriteFactory.CreateResult(propertyId: Guid.Empty);

        Assert.Equal(FavoriteErrors.PropertyIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptyUserAndProperty_ReturnsPropertyIdRequiredFirst()
    {
        var result = FavoriteFactory.CreateResult(Guid.Empty, Guid.Empty);

        Assert.Equal(FavoriteErrors.PropertyIdRequired.Code, result.TopError.Code);
        Assert.Single(result.Errors);
    }
}
