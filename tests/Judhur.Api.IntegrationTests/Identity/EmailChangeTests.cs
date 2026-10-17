using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Features.Identity;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class EmailChangeTests(JudhurApiFactory factory)
{
    private const string RequestRoute = $"{Routes.Account}/me/email";
    private const string ConfirmRoute = $"{Routes.Account}/confirm-email-change";

    private readonly IdentityScenario _identity = new(factory);

    private static CancellationToken Ct => IdentityScenario.Ct;

    private static Task<HttpResponseMessage> RequestChangeAsync(TestUser user, string newEmail, string? password)
        => user.Client.PostJsonAsync(RequestRoute, new { newEmail, password }, Ct);

    private Task<HttpResponseMessage> ConfirmChangeAsync(Guid userId, string newEmail, string token)
        => _identity.Anonymous().PostJsonAsync(ConfirmRoute, new { userId, newEmail, token }, Ct);

    private async Task<(Guid UserId, string NewEmail, string Token)> RequestLinkAsync(TestUser user, string newEmail)
    {
        var response = await RequestChangeAsync(user, newEmail, user.Password);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var message = await _identity.Emails.WaitForAsync(newEmail, Scenario.ConfirmEmailChangeSubject);
        var query = FakeEmailSender.QueryOf(IdentityScenario.ActionLink(message));
        return (Guid.Parse(query["userId"]), query["email"], query["token"]);
    }

    [Fact]
    public async Task RequestEmailChange_ValidRequest_Returns204AndSendsLinkToNewAddressOnly()
    {
        var user = await _identity.NewUserAsync();
        var newEmail = Scenario.NewEmail("changed");

        var response = await RequestChangeAsync(user, newEmail, user.Password);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var message = await _identity.Emails.WaitForAsync(newEmail, Scenario.ConfirmEmailChangeSubject);
        var link = IdentityScenario.ActionLink(message);
        Assert.Equal("http://localhost:5173/confirm-email-change", link.GetLeftPart(UriPartial.Path));
        var query = FakeEmailSender.QueryOf(link);
        Assert.Equal(user.Id, Guid.Parse(query["userId"]));
        Assert.Equal(newEmail, query["email"]);
        var me = await IdentityScenario.MeAsync(user.Client);
        Assert.Equal(user.Email, me.GetProperty("email").GetString());
        await _identity.LoginOkAsync(user.Email);
    }

    [Fact]
    public async Task ConfirmEmailChange_ValidLink_Returns204AndMovesLoginToNewAddress()
    {
        var user = await _identity.NewUserAsync();
        var newEmail = Scenario.NewEmail("changed");
        var (userId, linkEmail, token) = await RequestLinkAsync(user, newEmail);

        var response = await ConfirmChangeAsync(userId, linkEmail, token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await user.Client.GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
        var oldLogin = await _identity.LoginAsync(user.Email, user.Password);
        await oldLogin.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        var tokens = await _identity.LoginOkAsync(newEmail, user.Password);
        var me = await IdentityScenario.MeAsync(_identity.Authorized(tokens.AccessToken));
        Assert.Equal(newEmail, me.GetProperty("email").GetString());
        Assert.Equal(user.Id, me.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task ConfirmEmailChange_ValidLink_NotifiesPreviousAddress()
    {
        var user = await _identity.NewUserAsync();
        var newEmail = Scenario.NewEmail("changed");
        var (userId, linkEmail, token) = await RequestLinkAsync(user, newEmail);

        var response = await ConfirmChangeAsync(userId, linkEmail, token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var notice = await _identity.Emails.WaitForAsync(user.Email, IdentityScenario.EmailChangedSubject);
        Assert.Contains(newEmail, notice.HtmlMessage);
    }

    [Fact]
    public async Task ConfirmEmailChange_ValidLink_RevokesExistingSessions()
    {
        var user = await _identity.NewUserAsync();
        var (userId, linkEmail, token) = await RequestLinkAsync(user, Scenario.NewEmail("changed"));

        var response = await ConfirmChangeAsync(userId, linkEmail, token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var refresh = await _identity.RefreshAsync(user.Tokens);
        await refresh.AssertProblemAsync(HttpStatusCode.Unauthorized, "RefreshTokenErrors.Expired");
    }

    [Fact]
    public async Task ConfirmEmailChange_LinkUsedTwice_Returns400InvalidConfirmationToken()
    {
        var user = await _identity.NewUserAsync();
        var (userId, linkEmail, token) = await RequestLinkAsync(user, Scenario.NewEmail("changed"));
        var first = await ConfirmChangeAsync(userId, linkEmail, token);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await ConfirmChangeAsync(userId, linkEmail, token);

        await second.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidConfirmationToken");
    }

    [Fact]
    public async Task ConfirmEmailChange_InvalidToken_Returns400InvalidConfirmationToken()
    {
        var user = await _identity.NewUserAsync();
        var newEmail = Scenario.NewEmail("changed");
        await RequestLinkAsync(user, newEmail);

        var response = await ConfirmChangeAsync(user.Id, newEmail, "not-a-real-token");

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidConfirmationToken");
        await _identity.LoginOkAsync(user.Email);
    }

    [Fact]
    public async Task ConfirmEmailChange_TokenForAnotherAddress_Returns400InvalidConfirmationToken()
    {
        var user = await _identity.NewUserAsync();
        var (userId, _, token) = await RequestLinkAsync(user, Scenario.NewEmail("changed"));

        var response = await ConfirmChangeAsync(userId, Scenario.NewEmail("tampered"), token);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidConfirmationToken");
        await _identity.LoginOkAsync(user.Email);
    }

    [Fact]
    public async Task ConfirmEmailChange_AddressTakenMeanwhile_Returns409DuplicateEmail()
    {
        var user = await _identity.NewUserAsync();
        var newEmail = Scenario.NewEmail("changed");
        var (userId, linkEmail, token) = await RequestLinkAsync(user, newEmail);
        await _identity.RegisterUnconfirmedAsync(newEmail);

        var response = await ConfirmChangeAsync(userId, linkEmail, token);

        await response.AssertProblemAsync(HttpStatusCode.Conflict, "Identity.DuplicateEmail");
        await _identity.LoginOkAsync(user.Email);
    }

    [Fact]
    public async Task RequestEmailChange_AddressOfAnotherAccount_Returns409DuplicateEmail()
    {
        var user = await _identity.NewUserAsync();
        var other = Scenario.NewEmail("other");
        await _identity.RegisterUnconfirmedAsync(other);

        var response = await RequestChangeAsync(user, other, user.Password);

        await response.AssertProblemAsync(HttpStatusCode.Conflict, "Identity.DuplicateEmail");
    }

    [Fact]
    public async Task RequestEmailChange_SameAddressInDifferentCase_Returns400EmailUnchanged()
    {
        var user = await _identity.NewUserAsync();

        var response = await RequestChangeAsync(user, user.Email.ToUpperInvariant(), user.Password);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.EmailUnchanged");
    }

    [Fact]
    public async Task RequestEmailChange_WrongPassword_Returns400InvalidPasswordWithoutEmail()
    {
        var user = await _identity.NewUserAsync();
        var newEmail = Scenario.NewEmail("changed");

        var response = await RequestChangeAsync(user, newEmail, "Wr0ngPassword");

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidPassword");
        await _identity.AssertEmailCountStaysAsync(newEmail, Scenario.ConfirmEmailChangeSubject, 0);
    }

    [Fact]
    public async Task RequestEmailChange_WithoutPassword_Returns400CurrentPasswordRequired()
    {
        var user = await _identity.NewUserAsync();

        var response = await RequestChangeAsync(user, Scenario.NewEmail("changed"), null);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.CurrentPasswordRequired");
    }

    [Fact]
    public async Task RequestEmailChange_InvalidEmail_Returns400NewEmail()
    {
        var user = await _identity.NewUserAsync();

        var response = await RequestChangeAsync(user, "not-an-email", user.Password);

        await response.AssertValidationKeysAsync("NewEmail");
    }

    [Fact]
    public async Task RequestEmailChange_GoogleAccountWithItsGoogleToken_Returns204()
    {
        var (user, idToken) = await _identity.NewGoogleUserAsync();
        var newEmail = Scenario.NewEmail("changed");

        var response = await user.Client.PostJsonAsync(RequestRoute, new { newEmail, googleIdToken = idToken }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _identity.Emails.WaitForAsync(newEmail, Scenario.ConfirmEmailChangeSubject);
    }

    [Fact]
    public async Task RequestEmailChange_WithoutToken_Returns401()
    {
        var response = await _identity.Anonymous().PostJsonAsync(RequestRoute, new { newEmail = Scenario.NewEmail("changed"), password = Scenario.DefaultPassword }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmailChange_ThenLoginWithNewEmail_ReturnsTokensForSameUser()
    {
        var user = await _identity.NewUserAsync();
        var newEmail = Scenario.NewEmail("changed");
        var (userId, linkEmail, token) = await RequestLinkAsync(user, newEmail);
        await ConfirmChangeAsync(userId, linkEmail, token);

        var response = await _identity.LoginAsync(newEmail.ToUpperInvariant(), user.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.ReadAsync<TokenResponse>();
        var refresh = await _identity.RefreshAsync(tokens);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
    }
}
