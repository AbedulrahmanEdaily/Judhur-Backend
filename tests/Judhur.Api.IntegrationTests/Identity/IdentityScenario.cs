using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity;

namespace Judhur.Api.IntegrationTests.Identity;

public sealed partial class IdentityScenario(JudhurApiFactory factory)
{
    public const string PasswordChangedSubject = "تم تغيير كلمة المرور";
    public const string EmailChangedSubject = "تم تغيير البريد الإلكتروني";
    public const string NewPassword = "N3wPassw0rd!Test";
    public const string FullName = "مستخدم اختبار";
    public const string GoogleFullName = "مستخدم جوجل";

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public JudhurApiFactory Factory { get; } = factory;

    public Scenario Scenario { get; } = new(factory);

    public FakeEmailSender Emails => Factory.Emails;

    public HttpClient Anonymous() => Factory.CreateClient();

    public HttpClient Authorized(string accessToken) => TestUser.Authorize(Factory.CreateClient(), accessToken);

    public Task<HttpResponseMessage> RegisterAsync(string email, string password = Scenario.DefaultPassword)
        => Anonymous().PostJsonAsync($"{Routes.Account}/register", Scenario.RegisterBody(email, password, FullName), Ct);

    public async Task<(Guid UserId, string Token)> ReadConfirmationAsync(string email)
    {
        var message = await Emails.WaitForAsync(email, Scenario.ConfirmEmailSubject);
        return ConfirmationOf(message);
    }

    public static (Guid UserId, string Token) ConfirmationOf(EmailMessage message)
    {
        var query = FakeEmailSender.QueryOf(ActionLink(message));
        return (Guid.Parse(query["userId"]), query["token"]);
    }

    public Task<HttpResponseMessage> ConfirmEmailAsync(Guid userId, string token)
        => Anonymous().PostJsonAsync($"{Routes.Account}/confirm-email", new { userId, token }, Ct);

    public async Task<Guid> RegisterUnconfirmedAsync(string email, string password = Scenario.DefaultPassword)
    {
        var register = await RegisterAsync(email, password);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var (userId, _) = await ReadConfirmationAsync(email);
        return userId;
    }

    public async Task<Guid> RegisterConfirmedAsync(string email, string password = Scenario.DefaultPassword)
    {
        var register = await RegisterAsync(email, password);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var (userId, token) = await ReadConfirmationAsync(email);
        var confirm = await ConfirmEmailAsync(userId, token);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        return userId;
    }

    public Task<HttpResponseMessage> LoginAsync(string email, string password)
        => Anonymous().PostJsonAsync($"{Routes.Account}/login", new { email, password }, Ct);

    public async Task<TokenResponse> LoginOkAsync(string email, string password = Scenario.DefaultPassword)
    {
        var response = await LoginAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<TokenResponse>();
    }

    public async Task<TestUser> NewUserAsync(string prefix = "user")
    {
        var email = Scenario.NewEmail(prefix);
        var id = await RegisterConfirmedAsync(email);
        var tokens = await LoginOkAsync(email);
        return new TestUser(id, email, Scenario.DefaultPassword, FullName, tokens, Authorized(tokens.AccessToken));
    }

    public Task<HttpResponseMessage> RefreshAsync(string accessToken, string refreshToken)
        => Anonymous().PostJsonAsync($"{Routes.Account}/refresh-token", new { refreshToken, expiredAccessToken = accessToken }, Ct);

    public Task<HttpResponseMessage> RefreshAsync(TokenResponse tokens)
        => RefreshAsync(tokens.AccessToken, tokens.RefreshToken);

    public Task<HttpResponseMessage> LogoutAsync(string refreshToken)
        => Anonymous().PostJsonAsync($"{Routes.Account}/logout", new { refreshToken }, Ct);

    public Task<HttpResponseMessage> GoogleAsync(string idToken, string? phoneNumber = null, string? city = null)
        => Anonymous().PostJsonAsync($"{Routes.Account}/google", new { idToken, phoneNumber, city }, Ct);

    public static string GoogleToken(string email, bool emailVerified = true)
        => FakeGoogleTokenValidator.TokenFor(Guid.NewGuid().ToString("N"), email, GoogleFullName, emailVerified);

    public async Task<(TestUser User, string IdToken)> NewGoogleUserAsync(string prefix = "google")
    {
        var email = Scenario.NewEmail(prefix);
        var idToken = GoogleToken(email);
        var response = await GoogleAsync(idToken, Scenario.NewPhone(), "رام الله");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.ReadAsync<TokenResponse>();
        var client = Authorized(tokens.AccessToken);
        var id = (await MeAsync(client)).GetProperty("id").GetGuid();
        return (new TestUser(id, email, string.Empty, GoogleFullName, tokens, client), idToken);
    }

    public static async Task<JsonElement> MeAsync(HttpClient client)
    {
        var response = await client.GetAsync($"{Routes.Account}/me", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public async Task BanAsync(Guid userId)
    {
        var admin = await Scenario.AdminAsync();
        var response = await admin.Client.PostJsonAsync($"{Routes.AdminUsers}/{userId}/ban", new { reason = "مخالفة شروط الاستخدام" }, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public async Task UnbanAsync(Guid userId)
    {
        var admin = await Scenario.AdminAsync();
        var response = await admin.Client.PostAsync($"{Routes.AdminUsers}/{userId}/unban", null, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public IReadOnlyList<EmailMessage> EmailsTo(string to, string subject)
        => [.. Emails.Sent.Where(m => string.Equals(m.To, to, StringComparison.OrdinalIgnoreCase) && m.Subject == subject)];

    public async Task<EmailMessage> WaitForEmailAsync(string to, string subject, int count)
    {
        var messages = await Scenario.WaitUntilAsync(() => Task.FromResult(EmailsTo(to, subject)), m => m.Count >= count);
        Assert.True(messages.Count >= count, $"Expected {count} '{subject}' emails to {to} but found {messages.Count}.");
        return messages[count - 1];
    }

    public async Task AssertEmailCountStaysAsync(string to, string subject, int count)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), Ct);
        Assert.Equal(count, EmailsTo(to, subject).Count);
    }

    public static Uri ActionLink(EmailMessage message)
    {
        var link = LinkPattern().Matches(message.HtmlMessage)
            .Select(match => new Uri(WebUtility.HtmlDecode(match.Groups["url"].Value)))
            .FirstOrDefault(uri => FakeEmailSender.QueryOf(uri).ContainsKey("token"));
        return link ?? throw new InvalidOperationException("The email has no action link.");
    }

    public static string PublicIdOf(string imageUrl)
    {
        var path = new Uri(imageUrl).AbsolutePath.TrimStart('/');
        return path.EndsWith(".jpg", StringComparison.Ordinal) ? path[..^".jpg".Length] : path;
    }

    [GeneratedRegex("href=\"(?<url>https?://[^\"]+)\"")]
    private static partial Regex LinkPattern();
}
