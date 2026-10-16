using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Features.Identity;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class SessionTests(JudhurApiFactory factory)
{
    private readonly IdentityScenario _identity = new(factory);

    [Fact]
    public async Task RefreshToken_ValidPair_ReturnsNewWorkingPair()
    {
        var user = await _identity.NewUserAsync();

        var response = await _identity.RefreshAsync(user.Tokens);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotated = await response.ReadAsync<TokenResponse>();
        Assert.NotEqual(user.Tokens.RefreshToken, rotated.RefreshToken);
        var me = await IdentityScenario.MeAsync(_identity.Authorized(rotated.AccessToken));
        Assert.Equal(user.Id, me.GetProperty("id").GetGuid());
        var again = await _identity.RefreshAsync(rotated);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_SameRefreshTokenTwice_SecondReturns401()
    {
        var user = await _identity.NewUserAsync();
        var first = await _identity.RefreshAsync(user.Tokens);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _identity.RefreshAsync(user.Tokens);

        await second.AssertProblemAsync(HttpStatusCode.Unauthorized, "RefreshTokenErrors.Expired");
    }

    [Fact]
    public async Task RefreshToken_UnknownRefreshToken_Returns401()
    {
        var user = await _identity.NewUserAsync();

        var response = await _identity.RefreshAsync(user.Tokens.AccessToken, Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "RefreshTokenErrors.Expired");
    }

    [Fact]
    public async Task RefreshToken_InvalidAccessToken_Returns401ExpiredAccessTokenInvalid()
    {
        var user = await _identity.NewUserAsync();

        var response = await _identity.RefreshAsync("not-a-jwt", user.Tokens.RefreshToken);

        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Auth.ExpiredAccessToken.Invalid");
    }

    [Fact]
    public async Task RefreshToken_RefreshTokenOfAnotherUser_Returns401AndKeepsItValid()
    {
        var owner = await _identity.NewUserAsync();
        var other = await _identity.NewUserAsync();

        var response = await _identity.RefreshAsync(other.Tokens.AccessToken, owner.Tokens.RefreshToken);

        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "RefreshTokenErrors.Expired");
        var ownerRefresh = await _identity.RefreshAsync(owner.Tokens);
        Assert.Equal(HttpStatusCode.OK, ownerRefresh.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_EmptyBody_Returns400()
    {
        var response = await _identity.RefreshAsync(string.Empty, string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ValidRefreshToken_Returns204AndRevokesIt()
    {
        var user = await _identity.NewUserAsync();

        var response = await _identity.LogoutAsync(user.Tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var refresh = await _identity.RefreshAsync(user.Tokens);
        await refresh.AssertProblemAsync(HttpStatusCode.Unauthorized, "RefreshTokenErrors.Expired");
    }

    [Fact]
    public async Task Logout_UnknownRefreshToken_Returns204()
    {
        var response = await _identity.LogoutAsync(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_SameRefreshTokenTwice_Returns204BothTimes()
    {
        var user = await _identity.NewUserAsync();
        await _identity.LogoutAsync(user.Tokens.RefreshToken);

        var response = await _identity.LogoutAsync(user.Tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EmptyRefreshToken_Returns400()
    {
        var response = await _identity.LogoutAsync(string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
