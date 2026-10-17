using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Api.IntegrationTests.Marketplace;
using Judhur.Application.Common;
using Judhur.Domain.Properties;
using Judhur.Infrastructure.Data;
using Judhur.Tests.Common.Factories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Judhur.Api.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public sealed class PropertyPersistenceTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static AppDbContext Db(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    private async Task<Property> SaveNewPropertyAsync(Guid sellerId)
    {
        var property = PropertyFactory.Create(new PropertyDetails { Title = Market.NewTitle() }, sellerId);
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        db.Properties.Add(property);
        await db.SaveChangesAsync(Ct);
        return property;
    }

    private async Task<byte[]> RowVersionAsync(Guid propertyId)
    {
        using var scope = factory.Services.CreateScope();
        return await Db(scope).Properties
            .IgnoreQueryFilters()
            .Where(p => p.Id == propertyId)
            .Select(p => EF.Property<byte[]>(p, RowVersions.PropertyName))
            .SingleAsync(Ct);
    }

    [Fact]
    public async Task Delete_ThroughApi_KeepsRowButHidesItBehindQueryFilter()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());

        var response = await seller.Client.DeleteAsync($"{Routes.Properties}/{id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = Db(scope);
        Assert.False(await db.Properties.AnyAsync(p => p.Id == id, Ct));
        var row = await db.Properties.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == id, Ct);
        Assert.True(row.IsDeleted);
        Assert.NotNull(row.DeletedAtUtc);
        Assert.False(row.IsActive);
        Assert.Equal(3, await db.PropertyImages.CountAsync(i => i.PropertyId == id, Ct));
    }

    [Fact]
    public async Task Delete_ThroughAggregate_HidesRowFromFilteredQueries()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var property = await SaveNewPropertyAsync(seller.Id);

        using (var scope = factory.Services.CreateScope())
        {
            var db = Db(scope);
            var loaded = await db.Properties.SingleAsync(p => p.Id == property.Id, Ct);
            Assert.False(loaded.Delete(DateTimeOffset.UtcNow).IsError);
            await db.SaveChangesAsync(Ct);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = Db(scope);
            Assert.Null(await db.Properties.FirstOrDefaultAsync(p => p.Id == property.Id, Ct));
            Assert.Equal(0, await db.Properties.CountAsync(p => p.SellerId == seller.Id, Ct));
            Assert.Equal(1, await db.Properties.IgnoreQueryFilters().CountAsync(p => p.SellerId == seller.Id, Ct));
        }
    }

    [Fact]
    public async Task AddImage_ThroughAggregate_ChangesPropertyRowVersionAndModifiedTime()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var property = await SaveNewPropertyAsync(seller.Id);
        var original = await RowVersionAsync(property.Id);
        DateTimeOffset modifiedBefore;

        using (var scope = factory.Services.CreateScope())
        {
            var db = Db(scope);
            var loaded = await db.Properties.Include(p => p.PropertyImages).SingleAsync(p => p.Id == property.Id, Ct);
            modifiedBefore = loaded.LastModifiedUtc;
            PropertyFactory.AddImage(loaded);
            await db.SaveChangesAsync(Ct);
        }

        var current = await RowVersionAsync(property.Id);
        Assert.NotEqual(original, current);
        using var check = factory.Services.CreateScope();
        var saved = await Db(check).Properties.AsNoTracking().Include(p => p.PropertyImages).SingleAsync(p => p.Id == property.Id, Ct);
        Assert.Single(saved.PropertyImages);
        Assert.True(saved.LastModifiedUtc >= modifiedBefore);
    }

    [Fact]
    public async Task RemoveImage_ThroughAggregate_ChangesPropertyRowVersion()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var property = PropertyFactory.Create(new PropertyDetails { Title = Market.NewTitle() }, seller.Id);
        PropertyFactory.AddImage(property);
        var removable = PropertyFactory.AddImage(property);
        using (var scope = factory.Services.CreateScope())
        {
            var db = Db(scope);
            db.Properties.Add(property);
            await db.SaveChangesAsync(Ct);
        }
        var original = await RowVersionAsync(property.Id);

        using (var scope = factory.Services.CreateScope())
        {
            var db = Db(scope);
            var loaded = await db.Properties.Include(p => p.PropertyImages).SingleAsync(p => p.Id == property.Id, Ct);
            Assert.False(loaded.RemoveImage(removable).IsError);
            await db.SaveChangesAsync(Ct);
        }

        Assert.NotEqual(original, await RowVersionAsync(property.Id));
    }

    [Fact]
    public async Task SaveChanges_StalePropertyAfterImageAdded_ThrowsConcurrencyException()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var property = await SaveNewPropertyAsync(seller.Id);
        using var first = factory.Services.CreateScope();
        using var second = factory.Services.CreateScope();
        var firstDb = Db(first);
        var secondDb = Db(second);
        var withImage = await firstDb.Properties.Include(p => p.PropertyImages).SingleAsync(p => p.Id == property.Id, Ct);
        var stale = await secondDb.Properties.SingleAsync(p => p.Id == property.Id, Ct);

        PropertyFactory.AddImage(withImage);
        await firstDb.SaveChangesAsync(Ct);
        Assert.False(stale.Deactivate().IsError);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondDb.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task SaveChanges_ThroughApi_StampsAuditColumnsWithActingUser()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var started = DateTimeOffset.UtcNow.AddMinutes(-1);
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());

        Property created;
        using (var scope = factory.Services.CreateScope())
        {
            created = await Db(scope).Properties.AsNoTracking().SingleAsync(p => p.Id == id, Ct);
        }
        Assert.Equal(seller.Id, created.CreatedBy);
        Assert.Equal(seller.Id, created.LastModifiedBy);
        Assert.InRange(created.CreatedAtUtc, started, DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.True(created.LastModifiedUtc >= created.CreatedAtUtc);

        await Scenario.ApproveAsync(admin.Client, id);

        using var after = factory.Services.CreateScope();
        var approved = await Db(after).Properties.AsNoTracking().SingleAsync(p => p.Id == id, Ct);
        Assert.Equal(seller.Id, approved.CreatedBy);
        Assert.Equal(created.CreatedAtUtc, approved.CreatedAtUtc);
        Assert.Equal(admin.Id, approved.LastModifiedBy);
        Assert.Equal(admin.Id, approved.ReviewedBy);
        Assert.True(approved.LastModifiedUtc >= created.LastModifiedUtc);
    }

    [Fact]
    public async Task SaveChanges_OutsideRequest_StampsTimesWithoutUser()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var started = DateTimeOffset.UtcNow.AddMinutes(-1);

        var property = await SaveNewPropertyAsync(seller.Id);

        using var scope = factory.Services.CreateScope();
        var saved = await Db(scope).Properties.AsNoTracking().SingleAsync(p => p.Id == property.Id, Ct);
        Assert.Null(saved.CreatedBy);
        Assert.Null(saved.LastModifiedBy);
        Assert.InRange(saved.CreatedAtUtc, started, DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Equal(saved.CreatedAtUtc, saved.LastModifiedUtc);
    }
}
