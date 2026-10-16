using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

using static Judhur.Api.IntegrationTests.Social.SocialApi;

namespace Judhur.Api.IntegrationTests.Social;

[Collection(ApiCollection.Name)]
public sealed class NotificationsTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task List_NewUser_IsEmptyWithZeroUnread()
    {
        var user = await _scenario.NewUserAsync("user");

        var page = await GetJsonAsync(user.Client, Routes.Notifications);

        Assert.Equal(0, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, Scenario.Items(page).GetArrayLength());
        Assert.Equal(0, await UnreadNotificationsAsync(user.Client));
    }

    [Fact]
    public async Task List_AfterApproval_ContainsUnreadPropertyApproved()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var listing = await PublishedListingAsync(_scenario, seller);

        var notification = await WaitForNotificationAsync(seller.Client, "PropertyApproved", listing.Id);

        Assert.Equal("تمت الموافقة على عقارك", Text(notification, "title"));
        Assert.Contains(listing.Title, Text(notification, "body"));
        Assert.False(notification.GetProperty("isRead").GetBoolean());
        Assert.False(IsMissing(notification, "createdAtUtc"));
        Assert.Equal(1, await UnreadNotificationsAsync(seller.Client));
    }

    [Fact]
    public async Task List_SeveralNotifications_NewestFirstWithPaging()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var older = await PublishedListingAsync(_scenario, seller);
        var newer = await PublishedListingAsync(_scenario, seller);
        await WaitForNotificationsAsync(seller.Client, "PropertyApproved", newer.Id);

        var first = await GetJsonAsync(seller.Client, $"{Routes.Notifications}?page=1&pageSize=1");
        var second = await GetJsonAsync(seller.Client, $"{Routes.Notifications}?page=2&pageSize=1");

        Assert.Equal(2, first.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, first.GetProperty("totalPages").GetInt32());
        Assert.Equal(newer.Id, Scenario.Items(first)[0].GetProperty("referenceId").GetGuid());
        Assert.Equal(older.Id, Scenario.Items(second)[0].GetProperty("referenceId").GetGuid());
    }

    [Theory]
    [InlineData("pageSize=0", "Pagination.PageSizeInvalid")]
    [InlineData("pageSize=101", "Pagination.PageSizeInvalid")]
    [InlineData("page=0", "Pagination.PageInvalid")]
    public async Task List_InvalidPaging_Returns400(string query, string code)
    {
        var user = await _scenario.NewUserAsync("user");

        var response = await user.Client.GetAsync($"{Routes.Notifications}?{query}", Ct);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, code);
    }

    [Fact]
    public async Task MarkAsRead_OwnNotification_MarksItReadAndIsIdempotent()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var first = await PublishedListingAsync(_scenario, seller);
        var second = await PublishedListingAsync(_scenario, seller);
        var notification = await WaitForNotificationAsync(seller.Client, "PropertyApproved", first.Id);
        await WaitForNotificationAsync(seller.Client, "PropertyApproved", second.Id);
        var id = notification.GetProperty("id").GetGuid();
        Assert.Equal(2, await UnreadNotificationsAsync(seller.Client));

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Notifications}/{id}/read");
        var again = await PostEmptyAsync(seller.Client, $"{Routes.Notifications}/{id}/read");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Equal(1, await UnreadNotificationsAsync(seller.Client));
        Assert.True((await SingleAsync(seller.Client, Routes.Notifications, id)).GetProperty("isRead").GetBoolean());
        Assert.False((await WaitForNotificationAsync(seller.Client, "PropertyApproved", second.Id)).GetProperty("isRead").GetBoolean());
    }

    [Fact]
    public async Task MarkAsRead_SomeoneElsesNotification_Returns404AndLeavesItUnread()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var intruder = await _scenario.NewUserAsync("intruder");
        var listing = await PublishedListingAsync(_scenario, seller);
        var id = (await WaitForNotificationAsync(seller.Client, "PropertyApproved", listing.Id)).GetProperty("id").GetGuid();

        var response = await PostEmptyAsync(intruder.Client, $"{Routes.Notifications}/{id}/read");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "NotificationErrors.NotFound");
        Assert.Equal(1, await UnreadNotificationsAsync(seller.Client));
    }

    [Fact]
    public async Task MarkAsRead_UnknownNotification_Returns404()
    {
        var user = await _scenario.NewUserAsync("user");

        var response = await PostEmptyAsync(user.Client, $"{Routes.Notifications}/{Guid.NewGuid()}/read");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "NotificationErrors.NotFound");
    }

    [Fact]
    public async Task MarkAllAsRead_ClearsUnreadCountAndKeepsHistory()
    {
        var seller = await _scenario.NewUserAsync("seller");
        await PublishedListingAsync(_scenario, seller);
        var last = await PublishedListingAsync(_scenario, seller);
        await WaitForNotificationAsync(seller.Client, "PropertyApproved", last.Id);
        Assert.Equal(2, await UnreadNotificationsAsync(seller.Client));

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Notifications}/read-all");
        var again = await PostEmptyAsync(seller.Client, $"{Routes.Notifications}/read-all");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Equal(0, await UnreadNotificationsAsync(seller.Client));
        var page = await GetJsonAsync(seller.Client, Routes.Notifications);
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        Assert.All(Scenario.Items(page).EnumerateArray(), n => Assert.True(n.GetProperty("isRead").GetBoolean()));
    }

    [Fact]
    public async Task MarkAllAsRead_OnlyAffectsCaller()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var listing = await PublishedListingAsync(_scenario, seller);
        await WaitForNotificationAsync(seller.Client, "PropertyApproved", listing.Id);

        await PostEmptyAsync(other.Client, $"{Routes.Notifications}/read-all");

        Assert.Equal(1, await UnreadNotificationsAsync(seller.Client));
    }

    [Fact]
    public async Task Endpoints_AdminRole_AreAllowed()
    {
        var admin = await _scenario.AdminAsync();

        var list = await admin.Client.GetAsync($"{Routes.Notifications}?pageSize=1", Ct);
        var count = await admin.Client.GetAsync($"{Routes.Notifications}/unread-count", Ct);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, count.StatusCode);
    }

    [Fact]
    public async Task Endpoints_Anonymous_Return401()
    {
        var anonymous = _scenario.Anonymous();

        var list = await anonymous.GetAsync(Routes.Notifications, Ct);
        var count = await anonymous.GetAsync($"{Routes.Notifications}/unread-count", Ct);
        var read = await PostEmptyAsync(anonymous, $"{Routes.Notifications}/{Guid.NewGuid()}/read");
        var readAll = await PostEmptyAsync(anonymous, $"{Routes.Notifications}/read-all");

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, count.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, readAll.StatusCode);
    }
}
