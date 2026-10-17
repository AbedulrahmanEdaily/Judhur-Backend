using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Api.IntegrationTests.Marketplace;
using Judhur.Infrastructure.Data;
using Judhur.Tests.Common.Factories;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Judhur.Api.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public sealed class FavoritePersistenceTests(JudhurApiFactory factory)
{
    private const int UniqueIndexViolation = 2601;

    private readonly Scenario _scenario = new(factory);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveChanges_DuplicateFavorite_ViolatesUniqueIndex()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var property = PropertyFactory.Create(new PropertyDetails { Title = Market.NewTitle() }, seller.Id);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Properties.Add(property);
            db.Favorites.Add(FavoriteFactory.Create(buyer.Id, property.Id));
            await db.SaveChangesAsync(Ct);
        }

        using var duplicateScope = factory.Services.CreateScope();
        var duplicateDb = duplicateScope.ServiceProvider.GetRequiredService<AppDbContext>();
        duplicateDb.Favorites.Add(FavoriteFactory.Create(buyer.Id, property.Id));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => duplicateDb.SaveChangesAsync(Ct));
        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Equal(UniqueIndexViolation, sqlException.Number);
    }

    [Fact]
    public async Task SaveChanges_SameListingForDifferentUsers_IsAllowed()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var first = await _scenario.NewUserAsync("buyer");
        var second = await _scenario.NewUserAsync("buyer");
        var property = PropertyFactory.Create(new PropertyDetails { Title = Market.NewTitle() }, seller.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Properties.Add(property);
        db.Favorites.Add(FavoriteFactory.Create(first.Id, property.Id));
        db.Favorites.Add(FavoriteFactory.Create(second.Id, property.Id));
        await db.SaveChangesAsync(Ct);

        Assert.Equal(2, await db.Favorites.CountAsync(f => f.PropertyId == property.Id, Ct));
    }

    [Fact]
    public async Task Add_ConcurrentRequests_StoreOneFavoriteAndRejectTheOther()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var id = await Market.CreateApprovedAsync(seller.Client, admin.Client);

        var responses = await Task.WhenAll(
            buyer.Client.PostAsync($"{Routes.Favorites}/{id}", null, Ct),
            buyer.Client.PostAsync($"{Routes.Favorites}/{id}", null, Ct));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var favorite = Assert.Single(await db.Favorites.AsNoTracking().Where(f => f.UserId == buyer.Id).ToListAsync(Ct));
        Assert.Equal(id, favorite.PropertyId);
        Assert.Equal(buyer.Id, favorite.CreatedBy);
    }
}
