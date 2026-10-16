using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Features.Identity;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class LoginTests(JudhurApiFactory factory)
{
    private const string WrongPassword = "Wr0ngPassword";

    private readonly IdentityScenario _identity = new(factory);

    [Fact]
    public async Task Login_ConfirmedUser_Returns200WithWorkingTokens()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);

        var response = await _identity.LoginAsync(email, Scenario.DefaultPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.ReadAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.True(tokens.ExpiresOnUtc > DateTimeOffset.UtcNow);
        var me = await IdentityScenario.MeAsync(_identity.Authorized(tokens.AccessToken));
        Assert.Equal(email, me.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_EmailInDifferentCase_Returns200()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);

        var response = await _identity.LoginAsync(email.ToUpperInvariant(), Scenario.DefaultPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401InvalidCredentials()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);

        var response = await _identity.LoginAsync(email, WrongPassword);

        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401InvalidCredentials()
    {
        var response = await _identity.LoginAsync(Scenario.NewEmail("ghost"), Scenario.DefaultPassword);

        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
    }

    [Fact]
    public async Task Login_UnconfirmedUserWithRightPassword_Returns403EmailNotConfirmed()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterUnconfirmedAsync(email);

        var response = await _identity.LoginAsync(email, Scenario.DefaultPassword);

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.EmailNotConfirmed");
    }

    [Fact]
    public async Task Login_UnconfirmedUserWithWrongPassword_Returns401InvalidCredentials()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterUnconfirmedAsync(email);

        var response = await _identity.LoginAsync(email, WrongPassword);

        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
    }

    [Fact]
    public async Task Login_FiveWrongPasswords_LocksAccount()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var failed = await _identity.LoginAsync(email, WrongPassword);
            await failed.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        }
        var fifth = await _identity.LoginAsync(email, WrongPassword);
        var withRightPassword = await _identity.LoginAsync(email, Scenario.DefaultPassword);

        await fifth.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.LockedOut");
        await withRightPassword.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.LockedOut");
    }

    [Fact]
    public async Task Login_SuccessAfterFailures_ResetsFailureCount()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await _identity.LoginAsync(email, WrongPassword);
        }
        await _identity.LoginOkAsync(email);

        var afterReset = await _identity.LoginAsync(email, WrongPassword);

        await afterReset.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400Email()
    {
        var response = await _identity.LoginAsync("not-an-email", Scenario.DefaultPassword);

        await response.AssertValidationKeysAsync("Email");
    }
}
