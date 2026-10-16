using System.Net;
using System.Text.Json;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Notifications.Dto;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Notifications.Enums;

namespace Judhur.Api.IntegrationTests.Marketplace;

public static class Market
{
    public const string FilesHost = "https://files.test/";
    public const string PrivateFilesPrefix = "https://files.test/private/";

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static string NewTitle(string label = "عقار اختبار") => $"{label} {Guid.NewGuid():N}";

    public static string NewCity() => $"مدينة {Guid.NewGuid():N}";

    public static string PublicUrl(Guid propertyId) => $"{Routes.Properties}/{propertyId}";

    public static string MineUrl(Guid propertyId) => $"{Routes.Properties}/mine/{propertyId}";

    public static async Task<Guid> CreateApprovedAsync(HttpClient seller, HttpClient admin, ListingBody? body = null)
    {
        var id = await Scenario.CreateReadyPropertyAsync(seller, body ?? new ListingBody());
        await Scenario.ApproveAsync(admin, id);
        return id;
    }

    public static async Task<MyPropertyDetailsDto> GetMineAsync(HttpClient seller, Guid propertyId)
    {
        var response = await seller.GetAsync(MineUrl(propertyId), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<MyPropertyDetailsDto>();
    }

    public static async Task<List<MyPropertyDto>> GetMyListAsync(HttpClient seller)
    {
        var response = await seller.GetAsync($"{Routes.Properties}/mine", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<List<MyPropertyDto>>();
    }

    public static async Task<PaginatedList<PropertySummaryDto>> SearchAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"{Routes.Properties}?{query}", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<PaginatedList<PropertySummaryDto>>();
    }

    public static List<Guid> Ids(PaginatedList<PropertySummaryDto> page) => [.. page.Items!.Select(item => item.Id)];

    public static string Query(string key, string value) => $"{key}={Uri.EscapeDataString(value)}";

    public static async Task<HttpResponseMessage> PostEmptyAsync(HttpClient client, string url)
        => await client.PostAsync(url, null, Ct);

    public static async Task RejectAsync(HttpClient admin, Guid propertyId, string reason = "الصور غير واضحة")
    {
        var rowVersion = await Scenario.ReviewVersionAsync(admin, propertyId);
        var response = await admin.PostJsonAsync($"{Routes.AdminProperties}/{propertyId}/reject", new { rejectionReason = reason, rowVersion });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<PropertyForReviewDto> GetForReviewAsync(HttpClient admin, Guid propertyId)
    {
        var response = await admin.GetAsync($"{Routes.AdminProperties}/{propertyId}", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<PropertyForReviewDto>();
    }

    public static async Task<PendingPropertyDto?> FindPendingAsync(HttpClient admin, Guid propertyId)
    {
        for (var page = 1; ; page++)
        {
            var response = await admin.GetAsync($"{Routes.AdminProperties}/pending?page={page}&pageSize=100", Ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var list = await response.ReadAsync<PaginatedList<PendingPropertyDto>>();
            var match = list.Items!.FirstOrDefault(item => item.Id == propertyId);
            if (match is not null || page >= list.TotalPages)
            {
                return match;
            }
        }
    }

    public static async Task<List<NotificationDto>> NotificationsAboutAsync(HttpClient client, Guid referenceId, NotificationType type)
    {
        var matches = new List<NotificationDto>();
        for (var page = 1; ; page++)
        {
            var response = await client.GetAsync($"{Routes.Notifications}?page={page}&pageSize=100", Ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var list = await response.ReadAsync<PaginatedList<NotificationDto>>();
            matches.AddRange(list.Items!.Where(n => n.ReferenceId == referenceId && n.Type == type));
            if (page >= list.TotalPages)
            {
                return matches;
            }
        }
    }

    public static async Task BanAsync(HttpClient admin, Guid userId)
    {
        var response = await admin.PostJsonAsync($"{Routes.AdminUsers}/{userId}/ban", new { reason = "مخالفة شروط الاستخدام" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, await response.ReadErrorCodeAsync());
    }

    public static async Task AssertValidationErrorAsync(HttpResponseMessage response, params string[] keys)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadJsonAsync();
        var actual = problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).ToList();
        foreach (var key in keys)
        {
            Assert.Contains(actual, name => string.Equals(name, key, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static bool IsNullOrMissing(JsonElement element, string name)
        => !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null;

    public static string ImagePublicId(string imageUrl)
    {
        Assert.StartsWith(FilesHost, imageUrl, StringComparison.Ordinal);
        Assert.EndsWith(".jpg", imageUrl, StringComparison.Ordinal);
        return imageUrl[FilesHost.Length..^".jpg".Length];
    }

    public static string DocumentPublicId(string documentUrl)
    {
        Assert.StartsWith(PrivateFilesPrefix, documentUrl, StringComparison.Ordinal);
        var path = documentUrl[PrivateFilesPrefix.Length..];
        return path[..path.IndexOf('?', StringComparison.Ordinal)];
    }
}
