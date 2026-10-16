using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Domain.Notifications.Enums;

using static Judhur.Api.IntegrationTests.Marketplace.Market;

namespace Judhur.Api.IntegrationTests.Marketplace;

[Collection(ApiCollection.Name)]
public sealed class ReviewQueueNotificationTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task ListingBecomesReady_NotifiesAdminOnce()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var body = new ListingBody();
        var id = await Scenario.CreatePropertyAsync(seller.Client, body);
        await Scenario.AddImageAsync(seller.Client, id);
        await Scenario.AddImageAsync(seller.Client, id);
        await Scenario.UploadDocumentAsync(seller.Client, id);
        Assert.Empty(await NotificationsAboutAsync(admin.Client, id, NotificationType.PropertyPendingReview));

        await Scenario.AddImageAsync(seller.Client, id);

        var notification = Assert.Single(await NotificationsAboutAsync(admin.Client, id, NotificationType.PropertyPendingReview));
        Assert.Contains(body.Title, notification.Body, StringComparison.Ordinal);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task ChangesWhileAlreadyQueued_DoNotNotifyAgain()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        Assert.Single(await NotificationsAboutAsync(admin.Client, id, NotificationType.PropertyPendingReview));

        await Scenario.AddImageAsync(seller.Client, id);
        await Scenario.UploadDocumentAsync(seller.Client, id);
        Assert.Equal(HttpStatusCode.NoContent, (await seller.Client.PutJsonAsync($"{Routes.Properties}/{id}/description", new { description = "وصف محدث" })).StatusCode);

        Assert.Single(await NotificationsAboutAsync(admin.Client, id, NotificationType.PropertyPendingReview));
    }

    [Fact]
    public async Task OrderReadyFirstWithDocumentLast_NotifiesWhenDocumentArrives()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        for (var i = 0; i < 3; i++)
        {
            await Scenario.AddImageAsync(seller.Client, id);
        }
        Assert.Empty(await NotificationsAboutAsync(admin.Client, id, NotificationType.PropertyPendingReview));

        await Scenario.UploadDocumentAsync(seller.Client, id);

        Assert.Single(await NotificationsAboutAsync(admin.Client, id, NotificationType.PropertyPendingReview));
    }

    [Fact]
    public async Task ResubmitAfterRejection_NotifiesAdminAgain()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        await RejectAsync(admin.Client, id);

        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/resubmit")).StatusCode);

        Assert.Equal(2, (await NotificationsAboutAsync(admin.Client, id, NotificationType.PropertyPendingReview)).Count);
    }

    [Fact]
    public async Task ApprovedListingEditedBackToPending_NotifiesAdminAgain()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var body = new ListingBody();
        var id = await CreateApprovedAsync(seller.Client, admin.Client, body);

        Assert.Equal(HttpStatusCode.NoContent, (await seller.Client.PutJsonAsync($"{Routes.Properties}/{id}/details", (body with { Price = 125_000m }).Details())).StatusCode);

        Assert.Equal(2, (await NotificationsAboutAsync(admin.Client, id, NotificationType.PropertyPendingReview)).Count);
        Assert.NotNull(await FindPendingAsync(admin.Client, id));
    }

    [Fact]
    public async Task ReactivatingQueuedListing_NotifiesAdminAgain()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/deactivate")).StatusCode);
        Assert.Null(await FindPendingAsync(admin.Client, id));

        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/reactivate")).StatusCode);

        Assert.NotNull(await FindPendingAsync(admin.Client, id));
        Assert.Equal(2, (await NotificationsAboutAsync(admin.Client, id, NotificationType.PropertyPendingReview)).Count);
    }

    [Fact]
    public async Task ListingBecomesReady_DoesNotNotifyTheSeller()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());

        Assert.Empty(await NotificationsAboutAsync(seller.Client, id, NotificationType.PropertyPendingReview));
    }
}
