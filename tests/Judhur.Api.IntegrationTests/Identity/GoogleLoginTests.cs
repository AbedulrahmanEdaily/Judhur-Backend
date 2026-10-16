using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Features.Identity;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class GoogleLoginTests(JudhurApiFactory factory)
{
    private const string City = "رام الله";

    private readonly IdentityScenario _identity = new(factory);

    private async Task<System.Text.Json.JsonElement> MeOfAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.ReadAsync<TokenResponse>();
        return await IdentityScenario.MeAsync(_identity.Authorized(tokens.AccessToken));
    }

    [Fact]
    public async Task GoogleLogin_NewUserWithoutPhoneAndCity_Returns400RegistrationIncomplete()
    {
        var idToken = IdentityScenario.GoogleToken(Scenario.NewEmail("google"));

        var response = await _identity.GoogleAsync(idToken);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.GoogleRegistrationIncomplete");
    }

    [Fact]
    public async Task GoogleLogin_NewUserWithPhoneOnly_Returns400RegistrationIncomplete()
    {
        var idToken = IdentityScenario.GoogleToken(Scenario.NewEmail("google"));

        var response = await _identity.GoogleAsync(idToken, Scenario.NewPhone(), "  ");

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.GoogleRegistrationIncomplete");
    }

    [Fact]
    public async Task GoogleLogin_NewUserWithPhoneAndCity_Returns200AndCreatesConfirmedAccount()
    {
        var email = Scenario.NewEmail("google");
        var phone = Scenario.NewPhone();
        var idToken = IdentityScenario.GoogleToken(email);
        var incomplete = await _identity.GoogleAsync(idToken);
        Assert.Equal(HttpStatusCode.BadRequest, incomplete.StatusCode);

        var me = await MeOfAsync(await _identity.GoogleAsync(idToken, phone, City));

        Assert.Equal(email, me.GetProperty("email").GetString());
        Assert.Equal(IdentityScenario.GoogleFullName, me.GetProperty("fullName").GetString());
        Assert.Equal(phone, me.GetProperty("phoneNumber").GetString());
        Assert.Equal(City, me.GetProperty("city").GetString());
        Assert.False(me.GetProperty("hasPassword").GetBoolean());
        Assert.Equal(new[] { "User" }, me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()!));
    }

    [Fact]
    public async Task GoogleLogin_ReturningUser_Returns200ForSameAccountWithoutPhoneAndCity()
    {
        var (user, idToken) = await _identity.NewGoogleUserAsync();

        var me = await MeOfAsync(await _identity.GoogleAsync(idToken));

        Assert.Equal(user.Id, me.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GoogleLogin_ExistingConfirmedEmail_LinksAccountAndKeepsPassword()
    {
        var email = Scenario.NewEmail();
        var userId = await _identity.RegisterConfirmedAsync(email);
        var idToken = IdentityScenario.GoogleToken(email);

        var me = await MeOfAsync(await _identity.GoogleAsync(idToken));

        Assert.Equal(userId, me.GetProperty("id").GetGuid());
        Assert.True(me.GetProperty("hasPassword").GetBoolean());
        Assert.Equal(IdentityScenario.FullName, me.GetProperty("fullName").GetString());
        await _identity.LoginOkAsync(email);
        var again = await MeOfAsync(await _identity.GoogleAsync(idToken));
        Assert.Equal(userId, again.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GoogleLogin_ExistingUnconfirmedEmail_ConfirmsAccountAndRemovesPassword()
    {
        var email = Scenario.NewEmail();
        var userId = await _identity.RegisterUnconfirmedAsync(email);

        var me = await MeOfAsync(await _identity.GoogleAsync(IdentityScenario.GoogleToken(email)));

        Assert.Equal(userId, me.GetProperty("id").GetGuid());
        Assert.False(me.GetProperty("hasPassword").GetBoolean());
        var passwordLogin = await _identity.LoginAsync(email, Scenario.DefaultPassword);
        await passwordLogin.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
    }

    [Fact]
    public async Task GoogleLogin_UnverifiedGoogleEmail_Returns403GoogleEmailNotVerified()
    {
        var idToken = IdentityScenario.GoogleToken(Scenario.NewEmail("google"), emailVerified: false);

        var response = await _identity.GoogleAsync(idToken, Scenario.NewPhone(), City);

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.GoogleEmailNotVerified");
    }

    [Fact]
    public async Task GoogleLogin_UnverifiedEmailOfExistingAccount_Returns403AndDoesNotLink()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterUnconfirmedAsync(email);

        var response = await _identity.GoogleAsync(IdentityScenario.GoogleToken(email, emailVerified: false));

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.GoogleEmailNotVerified");
        var login = await _identity.LoginAsync(email, Scenario.DefaultPassword);
        await login.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.EmailNotConfirmed");
    }

    [Fact]
    public async Task GoogleLogin_InvalidToken_Returns401InvalidGoogleToken()
    {
        var response = await _identity.GoogleAsync("not-a-google-token", Scenario.NewPhone(), City);

        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidGoogleToken");
    }

    [Fact]
    public async Task GoogleLogin_InvalidPhone_Returns400PhoneNumber()
    {
        var idToken = IdentityScenario.GoogleToken(Scenario.NewEmail("google"));

        var response = await _identity.GoogleAsync(idToken, "123", City);

        await response.AssertValidationKeysAsync("PhoneNumber");
    }

    [Fact]
    public async Task GoogleLogin_LockedOutAccount_Returns403LockedOut()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await _identity.LoginAsync(email, "Wr0ngPassword");
        }

        var response = await _identity.GoogleAsync(IdentityScenario.GoogleToken(email));

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.LockedOut");
    }
}
