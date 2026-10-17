using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class RegistrationTests(JudhurApiFactory factory)
{
    private readonly IdentityScenario _identity = new(factory);

    private static CancellationToken Ct => IdentityScenario.Ct;

    [Fact]
    public async Task Register_NewEmail_Returns201AndSendsConfirmationLink()
    {
        var email = Scenario.NewEmail();

        var response = await _identity.RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await _identity.Emails.WaitForAsync(email, Scenario.ConfirmEmailSubject);
        var link = IdentityScenario.ActionLink(message);
        Assert.Equal("http://localhost:5173/confirm-email", link.GetLeftPart(UriPartial.Path));
        var query = FakeEmailSender.QueryOf(link);
        Assert.NotEqual(Guid.Empty, Guid.Parse(query["userId"]));
        Assert.False(string.IsNullOrWhiteSpace(query["token"]));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409DuplicateEmail()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterUnconfirmedAsync(email);

        var response = await _identity.RegisterAsync(email);

        await response.AssertProblemAsync(HttpStatusCode.Conflict, "Identity.DuplicateEmail");
    }

    [Fact]
    public async Task Register_DuplicateEmailWithDifferentCase_Returns409DuplicateEmail()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);

        var response = await _identity.RegisterAsync(email.ToUpperInvariant());

        await response.AssertProblemAsync(HttpStatusCode.Conflict, "Identity.DuplicateEmail");
    }

    [Fact]
    public async Task Register_InvalidFields_Returns400WithFieldKeys()
    {
        var body = new { fullName = "مستخدم", email = "not-an-email", phoneNumber = "123", city = "نابلس", bio = (string?)null, password = "short" };

        var response = await _identity.Anonymous().PostJsonAsync($"{Routes.Account}/register", body, Ct);

        await response.AssertValidationKeysAsync("Email", "PhoneNumber", "Password");
    }

    [Fact]
    public async Task Register_PasswordWithoutUppercaseOrDigit_Returns400Password()
    {
        var response = await _identity.RegisterAsync(Scenario.NewEmail(), "onlylowercase");

        await response.AssertValidationKeysAsync("Password");
    }

    [Fact]
    public async Task ConfirmEmail_ValidToken_Returns204AndAllowsLogin()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterUnconfirmedAsync(email);
        var (userId, token) = await _identity.ReadConfirmationAsync(email);

        var response = await _identity.ConfirmEmailAsync(userId, token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var login = await _identity.LoginAsync(email, Scenario.DefaultPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_InvalidToken_Returns400InvalidConfirmationToken()
    {
        var email = Scenario.NewEmail();
        var userId = await _identity.RegisterUnconfirmedAsync(email);

        var response = await _identity.ConfirmEmailAsync(userId, "not-a-real-token");

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidConfirmationToken");
        var login = await _identity.LoginAsync(email, Scenario.DefaultPassword);
        await login.AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.EmailNotConfirmed");
    }

    [Fact]
    public async Task ConfirmEmail_UnknownUser_Returns400InvalidConfirmationToken()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterUnconfirmedAsync(email);
        var (_, token) = await _identity.ReadConfirmationAsync(email);

        var response = await _identity.ConfirmEmailAsync(Guid.NewGuid(), token);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidConfirmationToken");
    }

    [Fact]
    public async Task ConfirmEmail_TokenOfAnotherUser_Returns400InvalidConfirmationToken()
    {
        var first = Scenario.NewEmail();
        var second = Scenario.NewEmail();
        await _identity.RegisterUnconfirmedAsync(first);
        var secondId = await _identity.RegisterUnconfirmedAsync(second);
        var (_, firstToken) = await _identity.ReadConfirmationAsync(first);

        var response = await _identity.ConfirmEmailAsync(secondId, firstToken);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Identity.InvalidConfirmationToken");
    }

    [Fact]
    public async Task ResendConfirmation_UnconfirmedUser_Returns204AndSendsWorkingLink()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterUnconfirmedAsync(email);

        var response = await _identity.Anonymous().PostJsonAsync($"{Routes.Account}/resend-confirmation", new { email }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var resent = await _identity.WaitForEmailAsync(email, Scenario.ConfirmEmailSubject, 2);
        var (userId, token) = IdentityScenario.ConfirmationOf(resent);
        var confirm = await _identity.ConfirmEmailAsync(userId, token);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_UnknownEmail_Returns204WithoutSendingEmail()
    {
        var email = Scenario.NewEmail("ghost");

        var response = await _identity.Anonymous().PostJsonAsync($"{Routes.Account}/resend-confirmation", new { email }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _identity.AssertEmailCountStaysAsync(email, Scenario.ConfirmEmailSubject, 0);
    }

    [Fact]
    public async Task ResendConfirmation_ConfirmedUser_Returns204WithoutSendingEmail()
    {
        var email = Scenario.NewEmail();
        await _identity.RegisterConfirmedAsync(email);

        var response = await _identity.Anonymous().PostJsonAsync($"{Routes.Account}/resend-confirmation", new { email }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _identity.AssertEmailCountStaysAsync(email, Scenario.ConfirmEmailSubject, 1);
    }

    [Fact]
    public async Task ResendConfirmation_InvalidEmail_Returns400Email()
    {
        var response = await _identity.Anonymous().PostJsonAsync($"{Routes.Account}/resend-confirmation", new { email = "not-an-email" }, Ct);

        await response.AssertValidationKeysAsync("Email");
    }
}
