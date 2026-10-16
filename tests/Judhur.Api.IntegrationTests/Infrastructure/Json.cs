using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Judhur.Api.IntegrationTests.Infrastructure;

public static class Json
{
    public static readonly JsonSerializerOptions Options = Create();

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Options);
        return value ?? throw new InvalidOperationException($"The response body could not be read as {typeof(T).Name}.");
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
        => await response.ReadAsync<JsonElement>();

    public static async Task<string?> ReadErrorCodeAsync(this HttpResponseMessage response)
    {
        var problem = await response.ReadJsonAsync();
        if (problem.TryGetProperty("code", out var code))
        {
            return code.GetString();
        }
        if (problem.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
        {
            return errors.EnumerateObject().Select(e => e.Name).FirstOrDefault(name => name.Contains('.'));
        }
        return null;
    }

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body)
        => client.PostAsJsonAsync(url, body, Options);

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T body)
        => client.PutAsJsonAsync(url, body, Options);

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
