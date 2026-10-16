using System.Net;
using System.Text.Json;

using Judhur.Api.IntegrationTests.Infrastructure;

using static Judhur.Api.IntegrationTests.Social.SocialApi;

namespace Judhur.Api.IntegrationTests.Admin;

[Collection(ApiCollection.Name)]
public sealed class AdminUsersTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    private static string Prefix() => $"adm{Guid.NewGuid():N}"[..15];

    private static async Task<List<Guid>> SearchIdsAsync(HttpClient admin, string search, string status)
    {
        var page = await GetJsonAsync(admin, $"{Routes.AdminUsers}?search={Uri.EscapeDataString(search)}&status={status}&pageSize=100");
        return Scenario.Items(page).EnumerateArray().Select(u => u.GetProperty("id").GetGuid()).ToList();
    }

    private static async Task<JsonElement> UserAsync(HttpClient admin, Guid userId)
        => await GetJsonAsync(admin, $"{Routes.AdminUsers}/{userId}");

    [Fact]
    public async Task List_SearchByEmail_ReturnsThatUserWithListingAndReportCounts()
    {
        var seller = await _scenario.NewUserAsync("searched", "بائع البحث");
        var reporter = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        await ReadyListingAsync(seller);
        await CreateReportAsync(reporter.Client, listing.Id);

        var page = await GetJsonAsync(admin.Client, $"{Routes.AdminUsers}?search={Uri.EscapeDataString(seller.Email)}");

        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
        var user = Scenario.Items(page)[0];
        Assert.Equal(seller.Id, user.GetProperty("id").GetGuid());
        Assert.Equal(seller.Email, Text(user, "email"));
        Assert.Equal("بائع البحث", Text(user, "fullName"));
        Assert.Equal("نابلس", Text(user, "city"));
        Assert.False(IsMissing(user, "phoneNumber"));
        Assert.False(user.GetProperty("isAdmin").GetBoolean());
        Assert.True(user.GetProperty("emailConfirmed").GetBoolean());
        Assert.True(IsMissing(user, "bannedAtUtc"));
        Assert.Equal(2, user.GetProperty("propertiesCount").GetInt32());
        Assert.Equal(1, user.GetProperty("activeListingsCount").GetInt32());
        Assert.Equal(1, user.GetProperty("pendingReportsOnListings").GetInt32());
    }

    [Fact]
    public async Task List_SearchByNameOrPhone_FindsUser()
    {
        var token = Guid.NewGuid().ToString("N")[..10];
        var user = await _scenario.NewUserAsync("named", $"مستخدم {token}");
        var admin = await _scenario.AdminAsync();
        var phone = Text(await UserAsync(admin.Client, user.Id), "phoneNumber");

        var byName = await SearchIdsAsync(admin.Client, token, "All");
        var byPhone = await SearchIdsAsync(admin.Client, phone, "All");

        Assert.Equal([user.Id], byName);
        Assert.Contains(user.Id, byPhone);
    }

    [Fact]
    public async Task List_StatusFilters_SeparateActiveBannedAndUnconfirmed()
    {
        var prefix = Prefix();
        var admin = await _scenario.AdminAsync();
        var active = await _scenario.NewUserAsync(prefix);
        var banned = await _scenario.NewUserAsync(prefix);
        var unconfirmedEmail = Scenario.NewEmail(prefix);
        Assert.Equal(HttpStatusCode.Created, (await _scenario.RegisterAsync(unconfirmedEmail)).StatusCode);
        var (unconfirmedId, _) = await _scenario.ReadConfirmationAsync(unconfirmedEmail);
        await BanAsync(admin.Client, banned.Id);

        var all = await SearchIdsAsync(admin.Client, prefix, "All");
        var activeOnly = await SearchIdsAsync(admin.Client, prefix, "Active");
        var bannedOnly = await SearchIdsAsync(admin.Client, prefix, "Banned");
        var unconfirmedOnly = await SearchIdsAsync(admin.Client, prefix, "Unconfirmed");

        Assert.Equal(3, all.Count);
        Assert.Equal([unconfirmedId, banned.Id, active.Id], all);
        Assert.Equal([active.Id], activeOnly);
        Assert.Equal([banned.Id], bannedOnly);
        Assert.Equal([unconfirmedId], unconfirmedOnly);
        Assert.False((await UserAsync(admin.Client, unconfirmedId)).GetProperty("emailConfirmed").GetBoolean());
    }

    [Theory]
    [InlineData("status=Deleted")]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    public async Task List_InvalidQuery_Returns400(string query)
    {
        var admin = await _scenario.AdminAsync();

        var response = await admin.Client.GetAsync($"{Routes.AdminUsers}?{query}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_SearchTooLong_Returns400()
    {
        var admin = await _scenario.AdminAsync();

        var response = await admin.Client.GetAsync($"{Routes.AdminUsers}?search={new string('a', 101)}", Ct);

        await AssertValidationErrorAsync(response, "Search");
    }

    [Fact]
    public async Task Details_ExistingUser_ReturnsSameShapeAsList()
    {
        var user = await _scenario.NewUserAsync("details");
        var admin = await _scenario.AdminAsync();

        var details = await UserAsync(admin.Client, user.Id);

        Assert.Equal(user.Id, details.GetProperty("id").GetGuid());
        Assert.Equal(user.Email, Text(details, "email"));
        Assert.Equal(0, details.GetProperty("propertiesCount").GetInt32());
        Assert.Equal(0, details.GetProperty("activeListingsCount").GetInt32());
        Assert.Equal(0, details.GetProperty("pendingReportsOnListings").GetInt32());
        Assert.False(IsMissing(details, "createdAtUtc"));
    }

    [Fact]
    public async Task Details_UnknownOrDeletedUser_Returns404()
    {
        var deleted = await _scenario.NewUserAsync("deleted");
        var admin = await _scenario.AdminAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAccountAsync(deleted)).StatusCode);

        var unknown = await admin.Client.GetAsync($"{Routes.AdminUsers}/{Guid.NewGuid()}", Ct);
        var gone = await admin.Client.GetAsync($"{Routes.AdminUsers}/{deleted.Id}", Ct);

        await AssertProblemAsync(unknown, HttpStatusCode.NotFound, "Auth.User.NotFound");
        await AssertProblemAsync(gone, HttpStatusCode.NotFound, "Auth.User.NotFound");
        Assert.Empty(await SearchIdsAsync(admin.Client, deleted.Email, "All"));
    }

    [Fact]
    public async Task Ban_User_RevokesSessionsDeactivatesListingsAndBlocksLogin()
    {
        var seller = await _scenario.NewUserAsync("banned");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        Assert.True(await IsInSearchAsync(_scenario.Anonymous(), listing.Token));

        var response = await admin.Client.PostJsonAsync($"{Routes.AdminUsers}/{seller.Id}/ban", new { reason = $"  {BanReason}  " });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await seller.Client.GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
        await AssertProblemAsync(await LoginAsync(_scenario, seller.Email), HttpStatusCode.Forbidden, "Identity.AccountBanned");
        var refresh = await _scenario.Anonymous().PostJsonAsync(
            $"{Routes.Account}/refresh-token",
            new { refreshToken = seller.Tokens.RefreshToken, expiredAccessToken = seller.Tokens.AccessToken });
        await AssertProblemAsync(refresh, HttpStatusCode.Forbidden, "Identity.AccountBanned");
        var details = await UserAsync(admin.Client, seller.Id);
        Assert.False(IsMissing(details, "bannedAtUtc"));
        Assert.Equal(BanReason, Text(details, "banReason"));
        Assert.Equal(1, details.GetProperty("propertiesCount").GetInt32());
        Assert.Equal(0, details.GetProperty("activeListingsCount").GetInt32());
        var review = await GetJsonAsync(admin.Client, $"{Routes.AdminProperties}/{listing.Id}");
        Assert.False(review.GetProperty("isActive").GetBoolean());
        Assert.Equal("Approved", Text(review, "moderationStatus"));
        Assert.False(await IsInSearchAsync(_scenario.Anonymous(), listing.Token));
        Assert.Equal(HttpStatusCode.NotFound, await PublicDetailsStatusAsync(_scenario.Anonymous(), listing.Id));
        await AssertProblemAsync(await _scenario.Anonymous().GetAsync($"{Routes.Sellers}/{seller.Id}", Ct), HttpStatusCode.NotFound, "Seller.NotFound");
    }

    [Fact]
    public async Task Ban_User_SendsAccountBannedNotification()
    {
        var seller = await _scenario.NewUserAsync("banned");
        var admin = await _scenario.AdminAsync();
        await PublishedListingAsync(_scenario, seller);
        await BanAsync(admin.Client, seller.Id, "إعلانات وهمية.");
        await UnbanAsync(admin.Client, seller.Id);
        var tokens = await _scenario.LoginAsync(seller.Email);
        var client = TestUser.Authorize(factory.CreateClient(), tokens.AccessToken);

        var notification = await WaitForNotificationAsync(client, "AccountBanned");

        Assert.Equal("تم حظر حسابك", Text(notification, "title"));
        Assert.Contains("السبب: إعلانات وهمية.", Text(notification, "body"));
        Assert.Contains("(1)", Text(notification, "body"));
        Assert.True(IsMissing(notification, "referenceId"));
    }

    [Fact]
    public async Task Ban_AdminAccount_Returns409CannotBanAdmin()
    {
        var admin = await _scenario.AdminAsync();
        var otherAdmin = await AdminAccounts.CreateExtraAdminAsync(factory);

        var response = await admin.Client.PostJsonAsync($"{Routes.AdminUsers}/{otherAdmin}/ban", new { reason = BanReason });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Users.CannotBanAdmin");
        var details = await UserAsync(admin.Client, otherAdmin);
        Assert.True(details.GetProperty("isAdmin").GetBoolean());
        Assert.True(IsMissing(details, "bannedAtUtc"));
    }

    [Fact]
    public async Task Ban_Self_Returns409CannotBanSelf()
    {
        var admin = await _scenario.AdminAsync();

        var response = await admin.Client.PostJsonAsync($"{Routes.AdminUsers}/{admin.Id}/ban", new { reason = BanReason });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Users.CannotBanSelf");
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Ban_AlreadyBanned_Returns409AlreadyBanned()
    {
        var user = await _scenario.NewUserAsync("banned");
        var admin = await _scenario.AdminAsync();
        await BanAsync(admin.Client, user.Id);

        var response = await admin.Client.PostJsonAsync($"{Routes.AdminUsers}/{user.Id}/ban", new { reason = "مرة ثانية" });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Users.AlreadyBanned");
        Assert.Equal(BanReason, Text(await UserAsync(admin.Client, user.Id), "banReason"));
    }

    [Fact]
    public async Task Ban_WithoutReason_Returns400AndDoesNotBan()
    {
        var user = await _scenario.NewUserAsync("user");
        var admin = await _scenario.AdminAsync();

        var empty = await admin.Client.PostJsonAsync($"{Routes.AdminUsers}/{user.Id}/ban", new { reason = " " });
        var tooLong = await admin.Client.PostJsonAsync($"{Routes.AdminUsers}/{user.Id}/ban", new { reason = new string('س', 501) });

        await AssertValidationErrorAsync(empty, "Reason");
        await AssertValidationErrorAsync(tooLong, "Reason");
        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task BanOrUnban_UnknownUser_Returns404()
    {
        var admin = await _scenario.AdminAsync();

        var ban = await admin.Client.PostJsonAsync($"{Routes.AdminUsers}/{Guid.NewGuid()}/ban", new { reason = BanReason });
        var unban = await PostEmptyAsync(admin.Client, $"{Routes.AdminUsers}/{Guid.NewGuid()}/unban");

        await AssertProblemAsync(ban, HttpStatusCode.NotFound, "Auth.User.NotFound");
        await AssertProblemAsync(unban, HttpStatusCode.NotFound, "Auth.User.NotFound");
    }

    [Fact]
    public async Task Unban_BannedUser_AllowsLoginKeepsListingsInactiveAndOldSessionsRevoked()
    {
        var seller = await _scenario.NewUserAsync("unbanned");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        await BanAsync(admin.Client, seller.Id);

        var response = await PostEmptyAsync(admin.Client, $"{Routes.AdminUsers}/{seller.Id}/unban");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await UserAsync(admin.Client, seller.Id);
        Assert.True(IsMissing(details, "bannedAtUtc"));
        Assert.True(IsMissing(details, "banReason"));
        Assert.Contains(seller.Id, await SearchIdsAsync(admin.Client, seller.Email, "Active"));
        var refresh = await _scenario.Anonymous().PostJsonAsync(
            $"{Routes.Account}/refresh-token",
            new { refreshToken = seller.Tokens.RefreshToken, expiredAccessToken = seller.Tokens.AccessToken });
        await AssertProblemAsync(refresh, HttpStatusCode.Unauthorized, "RefreshTokenErrors.Expired");
        var tokens = await _scenario.LoginAsync(seller.Email);
        var client = TestUser.Authorize(factory.CreateClient(), tokens.AccessToken);
        Assert.False((await MyListingAsync(client, listing.Id)).GetProperty("isActive").GetBoolean());
        Assert.False(await IsInSearchAsync(_scenario.Anonymous(), listing.Token));
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(client, $"{Routes.Properties}/{listing.Id}/reactivate")).StatusCode);
        Assert.True(await IsInSearchAsync(_scenario.Anonymous(), listing.Token));
        Assert.Equal(1, (await SellerPageAsync(_scenario.Anonymous(), seller.Id)).GetProperty("activeListingsCount").GetInt32());
    }

    [Fact]
    public async Task Unban_NotBanned_Returns409NotBanned()
    {
        var user = await _scenario.NewUserAsync("user");
        var admin = await _scenario.AdminAsync();

        var response = await PostEmptyAsync(admin.Client, $"{Routes.AdminUsers}/{user.Id}/unban");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Users.NotBanned");
    }

    [Theory]
    [InlineData(Routes.AdminUsers)]
    [InlineData(Routes.AdminStats)]
    [InlineData(Routes.AdminReports)]
    public async Task AdminEndpoints_UserRole_Returns403(string route)
    {
        var user = await _scenario.NewUserAsync("user");

        var response = await user.Client.GetAsync(route, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Routes.AdminUsers)]
    [InlineData(Routes.AdminStats)]
    [InlineData(Routes.AdminReports)]
    public async Task AdminEndpoints_Anonymous_Returns401(string route)
    {
        var response = await _scenario.Anonymous().GetAsync(route, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BanAndUnban_UserRole_Returns403()
    {
        var user = await _scenario.NewUserAsync("user");
        var target = await _scenario.NewUserAsync("target");

        var ban = await user.Client.PostJsonAsync($"{Routes.AdminUsers}/{target.Id}/ban", new { reason = BanReason });
        var unban = await PostEmptyAsync(user.Client, $"{Routes.AdminUsers}/{target.Id}/unban");

        Assert.Equal(HttpStatusCode.Forbidden, ban.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unban.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await target.Client.GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
    }
}
