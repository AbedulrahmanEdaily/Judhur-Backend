using System.Net;
using System.Text.Json;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Domain.Properties.Enums;

using static Judhur.Api.IntegrationTests.Social.SocialApi;

namespace Judhur.Api.IntegrationTests.Admin;

[Collection(ApiCollection.Name)]
public sealed class AdminStatsTests(JudhurApiFactory factory)
{
    private static readonly string[] UserCounters = ["totalUsers", "newUsers", "bannedUsers", "unconfirmedUsers"];
    private static readonly string[] PropertyCounters = ["published", "waitingForReview", "incomplete", "rejected", "inactive", "sold", "rented"];
    private static readonly string[] ActivityCounters = ["newUsers", "newListings", "messages", "reviews", "reports"];

    private readonly Scenario _scenario = new(factory);

    private static int Counter(JsonElement stats, string section, string name) => stats.GetProperty(section).GetProperty(name).GetInt32();

    private static void AssertIncreased(JsonElement before, JsonElement after, string section, string name, int atLeast)
    {
        var from = Counter(before, section, name);
        var to = Counter(after, section, name);
        Assert.True(to >= from + atLeast, $"{section}.{name} went from {from} to {to}; expected an increase of at least {atLeast}.");
    }

    [Fact]
    public async Task Get_Admin_ReturnsEverySectionWithNonNegativeCounters()
    {
        var admin = await _scenario.AdminAsync();

        var stats = await GetJsonAsync(admin.Client, Routes.AdminStats);

        Assert.All(UserCounters, name => Assert.True(Counter(stats, "users", name) >= 0));
        Assert.All(PropertyCounters, name => Assert.True(Counter(stats, "properties", name) >= 0));
        Assert.All(ActivityCounters, name => Assert.True(Counter(stats, "lastSevenDays", name) >= 0));
        Assert.True(stats.GetProperty("pendingReports").GetInt32() >= 0);
        Assert.True(Counter(stats, "users", "newUsers") <= Counter(stats, "users", "totalUsers"));
        Assert.Equal(Counter(stats, "users", "newUsers"), Counter(stats, "lastSevenDays", "newUsers"));
    }

    [Fact]
    public async Task Get_AfterActivity_CountersIncreaseByAtLeastThatActivity()
    {
        var admin = await _scenario.AdminAsync();
        var before = await GetJsonAsync(admin.Client, Routes.AdminStats);

        var seller = await _scenario.NewUserAsync("stats-seller");
        var buyer = await _scenario.NewUserAsync("stats-buyer");
        var banned = await _scenario.NewUserAsync("stats-banned");
        Assert.Equal(HttpStatusCode.Created, (await _scenario.RegisterAsync(Scenario.NewEmail("stats-unconfirmed"))).StatusCode);
        await Scenario.CreatePropertyAsync(seller.Client, ListingBody(NewToken()));
        await ReadyListingAsync(seller);
        var rejected = await ReadyListingAsync(seller);
        var rowVersion = await Scenario.ReviewVersionAsync(admin.Client, rejected.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.Client.PostJsonAsync($"{Routes.AdminProperties}/{rejected.Id}/reject", new { rejectionReason = "الوثيقة غير واضحة", rowVersion })).StatusCode);
        var sold = await PublishedListingAsync(_scenario, seller);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{sold.Id}/mark-sold")).StatusCode);
        var rentedId = await _scenario.CreateApprovedPropertyAsync(seller, ListingBody(NewToken(), PropertyStatus.ForRent));
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{rentedId}/mark-rented")).StatusCode);
        await PublishedListingAsync(_scenario, banned);
        await BanAsync(admin.Client, banned.Id);
        await CreateReportAsync(buyer.Client, sold.Id);
        await EligibleConversationAsync(buyer, seller, sold.Id);
        await CreateReviewAsync(buyer.Client, seller.Id, 4m);

        var after = await GetJsonAsync(admin.Client, Routes.AdminStats);

        AssertIncreased(before, after, "users", "totalUsers", 4);
        AssertIncreased(before, after, "users", "newUsers", 4);
        AssertIncreased(before, after, "users", "bannedUsers", 1);
        AssertIncreased(before, after, "users", "unconfirmedUsers", 1);
        AssertIncreased(before, after, "properties", "published", 2);
        AssertIncreased(before, after, "properties", "waitingForReview", 1);
        AssertIncreased(before, after, "properties", "incomplete", 1);
        AssertIncreased(before, after, "properties", "rejected", 1);
        AssertIncreased(before, after, "properties", "inactive", 1);
        AssertIncreased(before, after, "properties", "sold", 1);
        AssertIncreased(before, after, "properties", "rented", 1);
        Assert.True(after.GetProperty("pendingReports").GetInt32() >= before.GetProperty("pendingReports").GetInt32() + 1);
        AssertIncreased(before, after, "lastSevenDays", "newUsers", 4);
        AssertIncreased(before, after, "lastSevenDays", "newListings", 6);
        AssertIncreased(before, after, "lastSevenDays", "messages", 2);
        AssertIncreased(before, after, "lastSevenDays", "reviews", 1);
        AssertIncreased(before, after, "lastSevenDays", "reports", 1);
    }

    [Fact]
    public async Task Get_UserRole_Returns403()
    {
        var user = await _scenario.NewUserAsync("user");

        var response = await user.Client.GetAsync(Routes.AdminStats, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_Anonymous_Returns401()
    {
        var response = await _scenario.Anonymous().GetAsync(Routes.AdminStats, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
