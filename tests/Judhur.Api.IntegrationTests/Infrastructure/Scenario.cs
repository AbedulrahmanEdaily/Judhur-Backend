using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Judhur.Application.Features.Identity;
using Judhur.Domain.Properties.Enums;

namespace Judhur.Api.IntegrationTests.Infrastructure;

public sealed class Scenario(JudhurApiFactory factory)
{
    public const string DefaultPassword = "Passw0rd!Test";
    public const string ConfirmEmailSubject = "تأكيد البريد الإلكتروني";
    public const string ResetCodeSubject = "رمز استعادة كلمة المرور";
    public const string ConfirmEmailChangeSubject = "تأكيد البريد الإلكتروني الجديد";

    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9];
    private static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%judhur test document\n%%EOF");

    public JudhurApiFactory Factory { get; } = factory;

    public HttpClient Anonymous() => Factory.CreateClient();

    public static string NewEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@judhur.test";

    public static string NewPhone() => "059" + Random.Shared.Next(1_000_000, 9_999_999).ToString();

    public static object RegisterBody(string email, string password = DefaultPassword, string fullName = "مستخدم اختبار")
        => new { fullName, email, phoneNumber = NewPhone(), city = "نابلس", bio = (string?)null, password };

    public async Task<HttpResponseMessage> RegisterAsync(string email, string password = DefaultPassword, string fullName = "مستخدم اختبار")
        => await Anonymous().PostJsonAsync($"{Routes.Account}/register", RegisterBody(email, password, fullName));

    public async Task<(Guid UserId, string Token)> ReadConfirmationAsync(string email)
    {
        var message = await Factory.Emails.WaitForAsync(email, ConfirmEmailSubject);
        var query = FakeEmailSender.QueryOf(FakeEmailSender.ExtractLink(message));
        return (Guid.Parse(query["userId"]), query["token"]);
    }

    public async Task<Guid> RegisterConfirmedAsync(string email, string password = DefaultPassword, string fullName = "مستخدم اختبار")
    {
        var register = await RegisterAsync(email, password, fullName);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var (userId, token) = await ReadConfirmationAsync(email);
        var confirm = await Anonymous().PostJsonAsync($"{Routes.Account}/confirm-email", new { userId, token });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        return userId;
    }

    public async Task<TokenResponse> LoginAsync(string email, string password = DefaultPassword)
    {
        var response = await Anonymous().PostJsonAsync($"{Routes.Account}/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<TokenResponse>();
    }

    public async Task<TestUser> NewUserAsync(string prefix = "user", string fullName = "مستخدم اختبار")
    {
        var email = NewEmail(prefix);
        var id = await RegisterConfirmedAsync(email, DefaultPassword, fullName);
        var tokens = await LoginAsync(email);
        return new TestUser(id, email, DefaultPassword, fullName, tokens, TestUser.Authorize(Factory.CreateClient(), tokens.AccessToken));
    }

    public async Task<TestUser> AdminAsync()
    {
        var tokens = await LoginAsync(JudhurApiFactory.AdminEmail, JudhurApiFactory.AdminPassword);
        var me = await TestUser.Authorize(Factory.CreateClient(), tokens.AccessToken).GetAsync($"{Routes.Account}/me");
        var id = (await me.ReadJsonAsync()).GetProperty("id").GetGuid();
        return new TestUser(id, JudhurApiFactory.AdminEmail, JudhurApiFactory.AdminPassword, "مدير الاختبار", tokens, TestUser.Authorize(Factory.CreateClient(), tokens.AccessToken));
    }

    public static object PropertyBody(
        string title = "شقة للبيع في نابلس",
        decimal price = 120_000m,
        PropertyStatus status = PropertyStatus.ForSale,
        PropertyType type = PropertyType.Apartment,
        string city = "نابلس")
        => new
        {
            title,
            description = "شقة واسعة قريبة من الخدمات",
            price,
            paymentType = PaymentType.Cash,
            propertyType = type,
            propertyStatus = status,
            area = 140m,
            city,
            region = "رفيديا",
            fullAddress = "نابلس - رفيديا - شارع الجامعة",
            latitude = 32.2211,
            longitude = 35.2544,
            landClassification = LandClassification.A,
            legalStatus = LegalStatus.Tabo
        };

    public static async Task<Guid> CreatePropertyAsync(HttpClient seller, object? body = null)
    {
        var response = await seller.PostJsonAsync(Routes.Properties, body ?? PropertyBody());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    public static MultipartFormDataContent ImageForm(bool isMainImage = false, string contentType = "image/jpeg", byte[]? content = null, string fileName = "photo.jpg")
    {
        var file = new ByteArrayContent(content ?? JpegBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent
        {
            { file, "file", fileName },
            { new StringContent(isMainImage ? "true" : "false"), "isMainImage" }
        };
    }

    public static MultipartFormDataContent DocumentForm(string contentType = "application/pdf", byte[]? content = null, string fileName = "ownership.pdf")
    {
        var file = new ByteArrayContent(content ?? PdfBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    public static async Task<Guid> AddImageAsync(HttpClient seller, Guid propertyId, bool isMainImage = false)
    {
        var response = await seller.PostAsync($"{Routes.Properties}/{propertyId}/images", ImageForm(isMainImage));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    public static async Task UploadDocumentAsync(HttpClient seller, Guid propertyId)
    {
        var response = await seller.PutAsync($"{Routes.Properties}/{propertyId}/ownership-document", DocumentForm());
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<Guid> CreateReadyPropertyAsync(HttpClient seller, object? body = null)
    {
        var id = await CreatePropertyAsync(seller, body);
        for (var i = 0; i < 3; i++)
        {
            await AddImageAsync(seller, id);
        }
        await UploadDocumentAsync(seller, id);
        return id;
    }

    public static async Task<string> ReviewVersionAsync(HttpClient admin, Guid propertyId)
    {
        var response = await admin.GetAsync($"{Routes.AdminProperties}/{propertyId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("rowVersion").GetString()!;
    }

    public static async Task ApproveAsync(HttpClient admin, Guid propertyId)
    {
        var rowVersion = await ReviewVersionAsync(admin, propertyId);
        var response = await admin.PostJsonAsync($"{Routes.AdminProperties}/{propertyId}/approve", new { rowVersion });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public async Task<Guid> CreateApprovedPropertyAsync(TestUser seller, object? body = null)
    {
        var id = await CreateReadyPropertyAsync(seller.Client, body);
        var admin = await AdminAsync();
        await ApproveAsync(admin.Client, id);
        return id;
    }

    public static async Task<T> WaitUntilAsync<T>(Func<Task<T>> probe, Func<T, bool> done, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (true)
        {
            var value = await probe();
            if (done(value) || DateTime.UtcNow >= deadline)
            {
                return value;
            }
            await Task.Delay(100);
        }
    }

    public static JsonElement Items(JsonElement page) => page.GetProperty("items");
}
