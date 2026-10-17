using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Features.Identity;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class SetMyPasswordTests(JudhurApiFactory factory)
{
    private const string PasswordRoute = $"{Routes.Account}/me/password";

    private readonly IdentityScenario _identity = new(factory);

    private static CancellationToken Ct => IdentityScenario.Ct;

    [Fact]
    public async Task SetMyPassword_CorrectCurrentPassword_Returns200AndReplacesPassword()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.PutJsonAsync(PasswordRoute, new { currentPassword = user.Password, newPassword = IdentityScenario.NewPassword }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var oldLogin = await _identity.LoginAsync(user.Email, user.Password);
        await oldLogin.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        await _identity.LoginOkAsync(user.Email, IdentityScenario.NewPassword);
        await _identity.Emails.WaitForAsync(user.Email, IdentityScenario.PasswordChangedSubject);
    }

    [Fact]
    public async Task SetMyPassword_Success_EndsOtherSessionsAndKeepsThisDeviceSignedIn()
    {
        var user = await _identity.NewUserAsync();
        var otherDevice = await _identity.LoginOkAsync(user.Email, user.Password);

        var response = await user.Client.PutJsonAsync(PasswordRoute, new { currentPassword = user.Password, newPassword = IdentityScenario.NewPassword }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.ReadAsync<TokenResponse>();
        Assert.Equal(HttpStatusCode.Unauthorized, (await user.Client.GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _identity.Authorized(otherDevice.AccessToken).GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _identity.RefreshAsync(otherDevice)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _identity.Authorized(tokens.AccessToken).GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _identity.RefreshAsync(tokens)).StatusCode);
    }

    [Fact]
    public async Task SetMyPassword_WithoutCurrentPassword_Returns400CurrentPasswordRequired()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.PutJsonAsync(PasswordRoute, new { newPassword = IdentityScenario.NewPassword }, Ct);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.CurrentPasswordRequired");
        await _identity.LoginOkAsync(user.Email, user.Password);
    }

    [Fact]
    public async Task SetMyPassword_WrongCurrentPassword_Returns400PasswordMismatch()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.PutJsonAsync(PasswordRoute, new { currentPassword = "Wr0ngPassword", newPassword = IdentityScenario.NewPassword }, Ct);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.PasswordMismatch");
        await _identity.LoginOkAsync(user.Email, user.Password);
    }

    [Fact]
    public async Task SetMyPassword_SameAsCurrent_Returns400NewPassword()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.PutJsonAsync(PasswordRoute, new { currentPassword = user.Password, newPassword = user.Password }, Ct);

        await response.AssertValidationKeysAsync("NewPassword");
    }

    [Fact]
    public async Task SetMyPassword_WeakNewPassword_Returns400NewPassword()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.PutJsonAsync(PasswordRoute, new { currentPassword = user.Password, newPassword = "weak" }, Ct);

        await response.AssertValidationKeysAsync("NewPassword");
    }

    [Fact]
    public async Task SetMyPassword_WithoutToken_Returns401()
    {
        var response = await _identity.Anonymous().PutJsonAsync(PasswordRoute, new { currentPassword = Scenario.DefaultPassword, newPassword = IdentityScenario.NewPassword }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetMyPassword_GoogleAccountWithoutGoogleToken_Returns400GoogleReauthenticationRequired()
    {
        var (user, _) = await _identity.NewGoogleUserAsync();

        var response = await user.Client.PutJsonAsync(PasswordRoute, new { newPassword = IdentityScenario.NewPassword }, Ct);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.GoogleReauthenticationRequired");
    }

    [Fact]
    public async Task SetMyPassword_GoogleAccountWithItsGoogleToken_Returns200AndEnablesPasswordLogin()
    {
        var (user, idToken) = await _identity.NewGoogleUserAsync();

        var response = await user.Client.PutJsonAsync(PasswordRoute, new { newPassword = IdentityScenario.NewPassword, googleIdToken = idToken }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.ReadAsync<TokenResponse>();
        var me = await IdentityScenario.MeAsync(_identity.Authorized(tokens.AccessToken));
        Assert.True(me.GetProperty("hasPassword").GetBoolean());
        await _identity.LoginOkAsync(user.Email, IdentityScenario.NewPassword);
    }

    [Fact]
    public async Task SetMyPassword_GoogleAccountWithAnotherGoogleAccountToken_Returns400GoogleReauthenticationFailed()
    {
        var (user, _) = await _identity.NewGoogleUserAsync();
        var (_, otherToken) = await _identity.NewGoogleUserAsync();

        var response = await user.Client.PutJsonAsync(PasswordRoute, new { newPassword = IdentityScenario.NewPassword, googleIdToken = otherToken }, Ct);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.GoogleReauthenticationFailed");
    }
}
