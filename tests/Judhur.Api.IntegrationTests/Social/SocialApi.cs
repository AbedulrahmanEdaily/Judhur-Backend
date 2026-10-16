using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Domain.Properties.Enums;

namespace Judhur.Api.IntegrationTests.Social;

public sealed record Listing(Guid Id, string Title, string Token);

public static class SocialApi
{
    public const string DeletedUserName = "مستخدم محذوف";
    public const string BanReason = "نشر إعلانات مضللة";

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static string NewToken() => Guid.NewGuid().ToString("N");

    public static string TitleFor(string token) => $"شقة اختبار {token}";

    public static object ListingBody(string token, PropertyStatus status = PropertyStatus.ForSale)
        => Scenario.PropertyBody(title: TitleFor(token), status: status);

    public static async Task<Listing> PublishedListingAsync(Scenario scenario, TestUser seller)
    {
        var token = NewToken();
        var id = await scenario.CreateApprovedPropertyAsync(seller, ListingBody(token));
        return new Listing(id, TitleFor(token), token);
    }

    public static async Task<Listing> ReadyListingAsync(TestUser seller)
    {
        var token = NewToken();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, ListingBody(token));
        return new Listing(id, TitleFor(token), token);
    }

    public static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static Task<HttpResponseMessage> PostEmptyAsync(HttpClient client, string url)
        => client.PostAsync(url, null, Ct);

    public static async Task<List<JsonElement>> CollectAsync(HttpClient client, string url, Func<JsonElement, bool> match, int maxPages = 50)
    {
        var results = new List<JsonElement>();
        var separator = url.Contains('?') ? '&' : '?';
        for (var page = 1; page <= maxPages; page++)
        {
            var body = await GetJsonAsync(client, $"{url}{separator}page={page}&pageSize=100");
            results.AddRange(Scenario.Items(body).EnumerateArray().Where(match));
            if (page >= body.GetProperty("totalPages").GetInt32())
            {
                break;
            }
        }
        return results;
    }

    public static async Task<JsonElement?> FindAsync(HttpClient client, string url, Guid id, int maxPages = 50)
    {
        var found = await CollectAsync(client, url, item => item.GetProperty("id").GetGuid() == id, maxPages);
        return found.Count == 0 ? null : found[0];
    }

    public static async Task<JsonElement> SingleAsync(HttpClient client, string url, Guid id)
    {
        var found = await FindAsync(client, url, id);
        Assert.True(found.HasValue, $"{id} was not found in {url}.");
        return found.Value;
    }

    public static bool IsMissing(JsonElement element, string name)
        => !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null;

    public static string Text(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    public static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, await response.ReadErrorCodeAsync());
    }

    public static async Task AssertValidationErrorAsync(HttpResponseMessage response, string key)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadJsonAsync();
        var keys = problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).ToList();
        Assert.Contains(keys, k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
    }

    public static Task<HttpResponseMessage> StartAsync(HttpClient buyer, Guid propertyId, string message)
        => buyer.PostJsonAsync(Routes.Conversations, new { propertyId, message });

    public static async Task<Guid> StartConversationAsync(HttpClient buyer, Guid propertyId, string message = "السلام عليكم، هل العقار ما زال متاحًا؟")
    {
        var response = await StartAsync(buyer, propertyId, message);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("conversationId").GetGuid();
    }

    public static Task<HttpResponseMessage> SendAsync(HttpClient sender, Guid conversationId, string content)
        => sender.PostJsonAsync($"{Routes.Conversations}/{conversationId}/messages", new { content });

    public static async Task<JsonElement> SendMessageAsync(HttpClient sender, Guid conversationId, string content)
    {
        var response = await SendAsync(sender, conversationId, content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static async Task<Guid> EligibleConversationAsync(TestUser buyer, TestUser seller, Guid propertyId)
    {
        var conversationId = await StartConversationAsync(buyer.Client, propertyId);
        await SendMessageAsync(seller.Client, conversationId, "نعم، ما زال متاحًا");
        return conversationId;
    }

    public static async Task<JsonElement> InboxItemAsync(HttpClient client, Guid conversationId)
        => await SingleAsync(client, Routes.Conversations, conversationId);

    public static async Task<int> UnreadMessagesAsync(HttpClient client)
        => (await GetJsonAsync(client, $"{Routes.Conversations}/unread-count")).GetProperty("count").GetInt32();

    public static Task<HttpResponseMessage> ReportAsync(HttpClient reporter, Guid propertyId, string reason = "Fraud", string? details = "السعر والصور لا تطابق العقار")
        => reporter.PostJsonAsync(Routes.Reports, new { propertyId, reason, details });

    public static async Task<Guid> CreateReportAsync(HttpClient reporter, Guid propertyId, string reason = "Fraud", string? details = "السعر والصور لا تطابق العقار")
    {
        var response = await ReportAsync(reporter, propertyId, reason, details);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    public static async Task<JsonElement> ReportDetailsAsync(HttpClient admin, Guid reportId)
        => await GetJsonAsync(admin, $"{Routes.AdminReports}/{reportId}");

    public static async Task<JsonElement> MyReportAsync(HttpClient reporter, Guid reportId)
        => await SingleAsync(reporter, $"{Routes.Reports}/mine", reportId);

    public static Task<HttpResponseMessage> ReviewAsync(HttpClient reviewer, Guid sellerId, decimal rating, string? comment = "تعامل محترم وسريع")
        => reviewer.PostJsonAsync(Routes.Reviews, new { sellerId, rating, comment });

    public static async Task<JsonElement> CreateReviewAsync(HttpClient reviewer, Guid sellerId, decimal rating, string? comment = "تعامل محترم وسريع")
    {
        var response = await ReviewAsync(reviewer, sellerId, rating, comment);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static async Task<JsonElement> ReviewStatusAsync(HttpClient client, Guid sellerId)
        => await GetJsonAsync(client, $"{Routes.Reviews}/sellers/{sellerId}/mine");

    public static async Task<JsonElement> SellerPageAsync(HttpClient client, Guid sellerId)
        => await GetJsonAsync(client, $"{Routes.Sellers}/{sellerId}");

    public static async Task BanAsync(HttpClient admin, Guid userId, string reason = BanReason)
    {
        var response = await admin.PostJsonAsync($"{Routes.AdminUsers}/{userId}/ban", new { reason });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task UnbanAsync(HttpClient admin, Guid userId)
    {
        var response = await PostEmptyAsync(admin, $"{Routes.AdminUsers}/{userId}/unban");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<bool> IsInSearchAsync(HttpClient client, string token)
    {
        var page = await GetJsonAsync(client, $"{Routes.Properties}?searchTerm={token}&pageSize=100");
        return Scenario.Items(page).EnumerateArray().Any(p => Text(p, "title").Contains(token, StringComparison.Ordinal));
    }

    public static async Task<HttpStatusCode> PublicDetailsStatusAsync(HttpClient client, Guid propertyId)
        => (await client.GetAsync($"{Routes.Properties}/{propertyId}", Ct)).StatusCode;

    public static async Task<JsonElement> MyListingAsync(HttpClient seller, Guid propertyId)
        => await GetJsonAsync(seller, $"{Routes.Properties}/mine/{propertyId}");

    public static Task<HttpResponseMessage> LoginAsync(Scenario scenario, string email, string password = Scenario.DefaultPassword)
        => scenario.Anonymous().PostJsonAsync($"{Routes.Account}/login", new { email, password });

    public static async Task<HttpResponseMessage> DeleteAccountAsync(TestUser user)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{Routes.Account}/me")
        {
            Content = JsonContent.Create(new { password = user.Password }, options: Json.Options)
        };
        return await user.Client.SendAsync(request, Ct);
    }

    public static async Task<List<JsonElement>> NotificationsAsync(HttpClient client, string type, Guid? referenceId = null)
        => await CollectAsync(
            client,
            Routes.Notifications,
            n => Text(n, "type") == type
                && (referenceId is null || (n.TryGetProperty("referenceId", out var reference) && reference.ValueKind == JsonValueKind.String && reference.GetGuid() == referenceId)),
            maxPages: 3);

    public static async Task<List<JsonElement>> WaitForNotificationsAsync(HttpClient client, string type, Guid? referenceId = null, int atLeast = 1)
    {
        var found = await Scenario.WaitUntilAsync(() => NotificationsAsync(client, type, referenceId), list => list.Count >= atLeast);
        Assert.True(found.Count >= atLeast, $"Expected at least {atLeast} {type} notification(s) for {referenceId}, found {found.Count}.");
        return found;
    }

    public static async Task<JsonElement> WaitForNotificationAsync(HttpClient client, string type, Guid? referenceId = null)
        => (await WaitForNotificationsAsync(client, type, referenceId))[0];

    public static async Task<int> UnreadNotificationsAsync(HttpClient client)
        => (await GetJsonAsync(client, $"{Routes.Notifications}/unread-count")).GetProperty("count").GetInt32();
}
