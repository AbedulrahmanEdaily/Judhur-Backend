using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class BannedUserTests(JudhurApiFactory factory)
{
    private readonly IdentityScenario _identity = new(factory);

    private static CancellationToken Ct => IdentityScenario.Ct;

    [Fact]
    public async Task Login_BannedUserWithRightPassword_Returns403AccountBanned()
    {
        var user = await _identity.NewUserAsync("banned");
        await _identity.BanAsync(user.Id);

        var response = await _identity.LoginAsync(user.Email, user.Password);

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.AccountBanned");
    }

    [Fact]
    public async Task Login_BannedUserWithWrongPassword_Returns401InvalidCredentials()
    {
        var user = await _identity.NewUserAsync("banned");
        await _identity.BanAsync(user.Id);

        var response = await _identity.LoginAsync(user.Email, "Wr0ngPassword");

        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
    }

    [Fact]
    public async Task RefreshToken_BannedUser_Returns403AccountBanned()
    {
        var user = await _identity.NewUserAsync("banned");
        await _identity.BanAsync(user.Id);

        var response = await _identity.RefreshAsync(user.Tokens);

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.AccountBanned");
    }

    [Fact]
    public async Task GetMe_BannedUserAccessToken_Returns401()
    {
        var user = await _identity.NewUserAsync("banned");
        await _identity.BanAsync(user.Id);

        var response = await user.Client.GetAsync($"{Routes.Account}/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_BannedUserEmail_Returns403AccountBanned()
    {
        var user = await _identity.NewUserAsync("banned");
        await _identity.BanAsync(user.Id);

        var response = await _identity.GoogleAsync(IdentityScenario.GoogleToken(user.Email));

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.AccountBanned");
    }

    [Fact]
    public async Task GoogleLogin_BannedLinkedGoogleAccount_Returns403AccountBanned()
    {
        var (user, idToken) = await _identity.NewGoogleUserAsync("banned");
        await _identity.BanAsync(user.Id);

        var response = await _identity.GoogleAsync(idToken);

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.AccountBanned");
    }

    [Fact]
    public async Task ConfirmEmailChange_BannedUser_Returns400InvalidConfirmationToken()
    {
        var user = await _identity.NewUserAsync("banned");
        var newEmail = Scenario.NewEmail("changed");
        var request = await user.Client.PostJsonAsync($"{Routes.Account}/me/email", new { newEmail, password = user.Password }, Ct);
        Assert.Equal(HttpStatusCode.NoContent, request.StatusCode);
        var message = await _identity.Emails.WaitForAsync(newEmail, Scenario.ConfirmEmailChangeSubject);
        var query = FakeEmailSender.QueryOf(IdentityScenario.ActionLink(message));
        await _identity.BanAsync(user.Id);

        var response = await _identity.Anonymous().PostJsonAsync($"{Routes.Account}/confirm-email-change", new { userId = user.Id, newEmail = query["email"], token = query["token"] }, Ct);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidConfirmationToken");
    }

    [Fact]
    public async Task Login_AfterUnban_Returns200()
    {
        var user = await _identity.NewUserAsync("banned");
        await _identity.BanAsync(user.Id);
        await _identity.UnbanAsync(user.Id);

        var tokens = await _identity.LoginOkAsync(user.Email, user.Password);

        await IdentityScenario.MeAsync(_identity.Authorized(tokens.AccessToken));
    }
}
