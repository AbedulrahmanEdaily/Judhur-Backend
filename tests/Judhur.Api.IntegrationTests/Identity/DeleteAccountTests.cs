using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class DeleteAccountTests(JudhurApiFactory factory)
{
    private const string MeRoute = $"{Routes.Account}/me";

    private readonly IdentityScenario _identity = new(factory);

    private static CancellationToken Ct => IdentityScenario.Ct;

    private static async Task DeleteOkAsync(TestUser user)
    {
        var response = await user.Client.DeleteJsonAsync(MeRoute, new { password = user.Password }, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_CorrectPassword_Returns204AndBlocksLogin()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.DeleteJsonAsync(MeRoute, new { password = user.Password }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var login = await _identity.LoginAsync(user.Email, user.Password);
        await login.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
    }

    [Fact]
    public async Task DeleteMe_CorrectPassword_RejectsExistingAccessToken()
    {
        var user = await _identity.NewUserAsync();

        await DeleteOkAsync(user);

        var response = await user.Client.GetAsync(MeRoute, Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_CorrectPassword_EndsRefreshSession()
    {
        var user = await _identity.NewUserAsync();

        await DeleteOkAsync(user);

        var refresh = await _identity.RefreshAsync(user.Tokens);
        await refresh.AssertProblemAsync(HttpStatusCode.Unauthorized, "RefreshTokenErrors.Expired");
    }

    [Fact]
    public async Task DeleteMe_CorrectPassword_FreesEmailForNewRegistration()
    {
        var user = await _identity.NewUserAsync();
        await DeleteOkAsync(user);

        var register = await _identity.RegisterAsync(user.Email, IdentityScenario.NewPassword);

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var message = await _identity.WaitForEmailAsync(user.Email, Scenario.ConfirmEmailSubject, 2);
        var (newId, token) = IdentityScenario.ConfirmationOf(message);
        Assert.NotEqual(user.Id, newId);
        var confirm = await _identity.ConfirmEmailAsync(newId, token);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        var tokens = await _identity.LoginOkAsync(user.Email, IdentityScenario.NewPassword);
        var me = await IdentityScenario.MeAsync(_identity.Authorized(tokens.AccessToken));
        Assert.Equal(newId, me.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task DeleteMe_WithProfileImage_RemovesImageFromStorage()
    {
        var user = await _identity.NewUserAsync();
        using var form = Scenario.ImageForm();
        var upload = await user.Client.PutAsync($"{MeRoute}/photo", form, Ct);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var publicId = IdentityScenario.PublicIdOf((await upload.ReadJsonAsync()).GetProperty("profileImageUrl").GetString()!);

        await DeleteOkAsync(user);

        var deleted = await Scenario.WaitUntilAsync(() => Task.FromResult(_identity.Factory.Files.Deleted), d => d.Contains(publicId));
        Assert.Contains(publicId, deleted);
    }

    [Fact]
    public async Task DeleteMe_WithoutPassword_Returns400CurrentPasswordRequired()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.DeleteJsonAsync(MeRoute, new { }, Ct);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.CurrentPasswordRequired");
        await IdentityScenario.MeAsync(user.Client);
    }

    [Fact]
    public async Task DeleteMe_WrongPassword_Returns400InvalidPasswordAndKeepsAccount()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.DeleteJsonAsync(MeRoute, new { password = "Wr0ngPassword" }, Ct);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidPassword");
        await IdentityScenario.MeAsync(user.Client);
        await _identity.LoginOkAsync(user.Email);
    }

    [Fact]
    public async Task DeleteMe_Admin_Returns409AdminCannotDeleteAccount()
    {
        var admin = await _identity.Scenario.AdminAsync();

        var response = await admin.Client.DeleteJsonAsync(MeRoute, new { }, Ct);

        await response.AssertProblemAsync(HttpStatusCode.Conflict, "Users.AdminCannotDeleteAccount");
        await IdentityScenario.MeAsync(admin.Client);
    }

    [Fact]
    public async Task DeleteMe_WithoutToken_Returns401()
    {
        var response = await _identity.Anonymous().DeleteJsonAsync(MeRoute, new { password = Scenario.DefaultPassword }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_GoogleAccountWithoutGoogleToken_Returns400GoogleReauthenticationRequired()
    {
        var (user, _) = await _identity.NewGoogleUserAsync();

        var response = await user.Client.DeleteJsonAsync(MeRoute, new { }, Ct);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.GoogleReauthenticationRequired");
    }

    [Fact]
    public async Task DeleteMe_GoogleAccountWithItsGoogleToken_Returns204AndUnlinksGoogle()
    {
        var (user, idToken) = await _identity.NewGoogleUserAsync();

        var response = await user.Client.DeleteJsonAsync(MeRoute, new { googleIdToken = idToken }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var signInAgain = await _identity.GoogleAsync(idToken);
        await signInAgain.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.GoogleRegistrationIncomplete");
    }
}
