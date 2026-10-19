using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Domain.Common;
using Judhur.Domain.Properties.Enums;
using Judhur.Domain.Reports.Enums;
using Judhur.Infrastructure.Data;
using Judhur.Infrastructure.Data.Seed;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using static Judhur.Api.IntegrationTests.Marketplace.Market;

namespace Judhur.Api.IntegrationTests.Persistence;

public sealed class DemoDataSeedTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private const string DemoSellerEmail = "user@judhur.com";
    private const string DemoPassword = "User@12345";

    private readonly Scenario _scenario = new(factory);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Startup_SeedsListingsInEveryState()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var properties = await db.Properties.Include(p => p.PropertyImages).ToListAsync(Ct);

        Assert.True(properties.Count >= 30);
        Assert.All(properties, p => Assert.Single(p.PropertyImages, i => i.IsMainImage));
        Assert.Equal(properties.Count, properties.Select(p => p.PropertyImages.Single(i => i.IsMainImage).FileUrl).Distinct().Count());
        Assert.Contains(properties, p => p.ModerationStatus == ModerationStatus.Pending && p.OwnershipDocumentPublicId is not null);
        Assert.Contains(properties, p => p.ModerationStatus == ModerationStatus.Pending && p.OwnershipDocumentPublicId is null);
        Assert.Contains(properties, p => p.ModerationStatus == ModerationStatus.Rejected);
        Assert.Contains(properties, p => p.PropertyStatus == PropertyStatus.Sold);
        Assert.Contains(properties, p => p.PropertyStatus == PropertyStatus.Rented);
        Assert.Contains(properties, p => !p.IsActive);
        Assert.All(
            properties.Where(p => p.ModerationStatus == ModerationStatus.Approved),
            p => Assert.True(p.PropertyImages.Count >= 3 && p.OwnershipDocumentPublicId is not null));
        Assert.All(
            properties.Where(p => p.OwnershipDocumentPublicId is not null),
            p => Assert.StartsWith($"judhur/ownership-documents/{p.Id}/", p.OwnershipDocumentPublicId));
        Assert.True(properties.Min(p => p.CreatedAtUtc) < DateTimeOffset.UtcNow.AddDays(-30));
    }

    [Fact]
    public async Task Startup_SeedsConsistentActivity()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversations = await db.Conversations.Include(c => c.Messages).ToListAsync(Ct);
        var reviews = await db.Reviews.ToListAsync(Ct);
        var reports = await db.Reports.ToListAsync(Ct);

        Assert.NotEmpty(conversations);
        Assert.All(conversations, c => Assert.NotEmpty(c.Messages));
        Assert.Contains(conversations, c => c.Messages.Any(m => !m.IsRead));
        Assert.NotEmpty(reviews);
        Assert.All(reviews, r => Assert.Contains(conversations, c =>
            c.BuyerId == r.ReviewerId && c.SellerId == r.SellerId && c.Messages.Any(m => m.SenderId == r.SellerId)));
        Assert.Contains(reports, r => r.Status == ReportStatus.Pending);
        Assert.Contains(reports, r => r.Status == ReportStatus.Resolved);
        Assert.Contains(reports, r => r.Status == ReportStatus.Dismissed);
        Assert.NotEmpty(await db.Favorites.ToListAsync(Ct));
        Assert.Contains(await db.Notifications.ToListAsync(Ct), n => !n.IsRead);
        Assert.Single(await db.Users.Where(u => u.BannedAtUtc != null).ToListAsync(Ct));
    }

    [Fact]
    public async Task DemoSeller_CanSignInAndFindListings()
    {
        await _scenario.LoginAsync(DemoSellerEmail, DemoPassword);

        var page = await SearchAsync(_scenario.Anonymous(), $"pageSize=100&{Query("city", "نابلس")}");

        Assert.NotEmpty(page.Items!);
    }

    [Fact]
    public async Task SeedAsync_SecondRun_AddsNothing()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adminId = await db.UserRoles
            .Where(ur => db.Roles.Any(r => r.Id == ur.RoleId && r.Name == Roles.Admin))
            .Select(ur => ur.UserId)
            .FirstAsync(Ct);
        var before = (await db.Properties.IgnoreQueryFilters().CountAsync(Ct), await db.Users.CountAsync(Ct), await db.Notifications.CountAsync(Ct));

        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(adminId, Ct);

        var after = (await db.Properties.IgnoreQueryFilters().CountAsync(Ct), await db.Users.CountAsync(Ct), await db.Notifications.CountAsync(Ct));
        Assert.Equal(before, after);
    }
}
