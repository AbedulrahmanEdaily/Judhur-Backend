using System.Net.Http.Headers;

using Judhur.Application.Features.Identity;

namespace Judhur.Api.IntegrationTests.Infrastructure;

public sealed record TestUser(Guid Id, string Email, string Password, string FullName, TokenResponse Tokens, HttpClient Client)
{
    public static HttpClient Authorize(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
