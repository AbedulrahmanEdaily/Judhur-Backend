using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Judhur.Api.IntegrationTests.Infrastructure;

namespace Judhur.Api.IntegrationTests.Identity;

public static class IdentityHttp
{
    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body, CancellationToken cancellationToken)
        => client.PostAsJsonAsync(url, body, Json.Options, cancellationToken);

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T body, CancellationToken cancellationToken)
        => client.PutAsJsonAsync(url, body, Json.Options, cancellationToken);

    public static async Task<HttpResponseMessage> DeleteJsonAsync<T>(this HttpClient client, string url, T body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Content = JsonContent.Create(body, options: Json.Options)
        };
        return await client.SendAsync(request, cancellationToken);
    }

    public static async Task<IReadOnlyCollection<string>> ReadErrorKeysAsync(this HttpResponseMessage response)
    {
        var problem = await response.ReadJsonAsync();
        return problem.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object
            ? [.. errors.EnumerateObject().Select(e => e.Name)]
            : [];
    }

    public static async Task AssertProblemAsync(this HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, await response.ReadErrorCodeAsync());
    }

    public static async Task AssertValidationKeysAsync(this HttpResponseMessage response, params string[] keys)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var actual = await response.ReadErrorKeysAsync();
        foreach (var key in keys)
        {
            Assert.Contains(key, actual, StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string? OptionalString(this JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
