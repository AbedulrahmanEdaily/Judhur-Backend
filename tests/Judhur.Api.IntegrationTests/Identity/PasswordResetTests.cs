using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class PasswordResetTests(JudhurApiFactory factory)
{
    private const string WrongCode = "000000";

    private readonly IdentityScenario _identity = new(factory);

    private static CancellationToken Ct => IdentityScenario.Ct;

    private Task<HttpResponseMessage> SendCodeAsync(string email)
        => _identity.Anonymous().PostJsonAsync($"{Routes.Account}/send-reset-password-code", new { email }, Ct);

    private Task<HttpResponseMessage> ChangePasswordAsync(string email, string code, string password = IdentityScenario.NewPassword)
        => _identity.Anonymous().PostJsonAsync($"{Routes.Account}/change-password", new { email, code, password }, Ct);

    private async Task<string> RequestCodeAsync(string email, int count = 1)
    {
        var response = await SendCodeAsync(email);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var message = await _identity.WaitForEmailAsync(email, Scenario.ResetCodeSubject, count);
        return FakeEmailSender.ExtractResetCode(message);
    }

    [Fact]
    public async Task SendResetPasswordCode_KnownEmail_Returns204AndEmailsSixDigitCode()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);

        var code = await RequestCodeAsync(email);

        Assert.Matches("^[1-9][0-9]{5}$", code);
    }

    [Fact]
    public async Task SendResetPasswordCode_UnknownEmail_Returns204WithoutSendingEmail()
    {
        var email = Scenario.NewEmail("ghost");

        var response = await SendCodeAsync(email);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _identity.AssertEmailCountStaysAsync(email, Scenario.ResetCodeSubject, 0);
    }

    [Fact]
    public async Task SendResetPasswordCode_InvalidEmail_Returns400Email()
    {
        var response = await SendCodeAsync("not-an-email");

        await response.AssertValidationKeysAsync("Email");
    }

    [Fact]
    public async Task ChangePassword_ValidCode_Returns204AndReplacesPassword()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);
        var code = await RequestCodeAsync(email);

        var response = await ChangePasswordAsync(email, code);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var oldLogin = await _identity.LoginAsync(email, Scenario.DefaultPassword);
        await oldLogin.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        await _identity.LoginOkAsync(email, IdentityScenario.NewPassword);
        await _identity.Emails.WaitForAsync(email, IdentityScenario.PasswordChangedSubject);
    }

    [Fact]
    public async Task ChangePassword_ValidCode_RevokesExistingSessions()
    {
        var user = await _identity.NewUserAsync();
        var code = await RequestCodeAsync(user.Email);

        var response = await ChangePasswordAsync(user.Email, code);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var refresh = await _identity.RefreshAsync(user.Tokens);
        await refresh.AssertProblemAsync(HttpStatusCode.Unauthorized, "RefreshTokenErrors.Expired");
    }

    [Fact]
    public async Task ChangePassword_WrongCode_Returns400InvalidResetCodeAndKeepsPassword()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);
        await RequestCodeAsync(email);

        var response = await ChangePasswordAsync(email, WrongCode);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidResetCode");
        await _identity.LoginOkAsync(email);
    }

    [Fact]
    public async Task ChangePassword_CodeUsedTwice_SecondReturns400InvalidResetCode()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);
        var code = await RequestCodeAsync(email);
        var first = await ChangePasswordAsync(email, code);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await ChangePasswordAsync(email, code, "An0therPassw0rd");

        await second.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidResetCode");
        await _identity.LoginOkAsync(email, IdentityScenario.NewPassword);
    }

    [Fact]
    public async Task ChangePassword_NoCodeRequested_Returns400InvalidResetCode()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);

        var response = await ChangePasswordAsync(email, "123456");

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidResetCode");
    }

    [Fact]
    public async Task ChangePassword_UnknownEmail_Returns400InvalidResetCode()
    {
        var response = await ChangePasswordAsync(Scenario.NewEmail("ghost"), "123456");

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidResetCode");
    }

    [Fact]
    public async Task ChangePassword_FiveWrongCodes_RejectsTheCorrectCode()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);
        var code = await RequestCodeAsync(email);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var wrong = await ChangePasswordAsync(email, WrongCode);
            await wrong.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidResetCode");
        }

        var response = await ChangePasswordAsync(email, code);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidResetCode");
        await _identity.LoginOkAsync(email);
    }

    [Fact]
    public async Task ChangePassword_NewCodeRequested_ResetsAttemptsAndAcceptsNewCode()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);
        await RequestCodeAsync(email);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await ChangePasswordAsync(email, WrongCode);
        }
        var newCode = await RequestCodeAsync(email, 2);

        var response = await ChangePasswordAsync(email, newCode);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_OlderCodeAfterNewRequest_Returns400InvalidResetCode()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);
        var oldCode = await RequestCodeAsync(email);
        var newCode = await RequestCodeAsync(email, 2);
        Assert.SkipWhen(oldCode == newCode, "Both requests produced the same random code.");

        var response = await ChangePasswordAsync(email, oldCode);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidResetCode");
    }

    [Fact]
    public async Task ChangePassword_UnconfirmedUser_ConfirmsEmail()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterUnconfirmedAsync(email);
        var code = await RequestCodeAsync(email);

        var response = await ChangePasswordAsync(email, code);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _identity.LoginOkAsync(email, IdentityScenario.NewPassword);
    }

    [Fact]
    public async Task ChangePassword_WeakPassword_Returns400Password()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);
        var code = await RequestCodeAsync(email);

        var response = await ChangePasswordAsync(email, code, "weak");

        await response.AssertValidationKeysAsync("Password");
    }
}
