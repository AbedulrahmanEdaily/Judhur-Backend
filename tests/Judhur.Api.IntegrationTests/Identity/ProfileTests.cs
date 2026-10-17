using System.Net;
using System.Text;

using Judhur.Api.IntegrationTests.Infrastructure;

namespace Judhur.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class ProfileTests(JudhurApiFactory factory)
{
    private const string MeRoute = $"{Routes.Account}/me";
    private const string PhotoRoute = $"{Routes.Account}/me/photo";

    private readonly IdentityScenario _identity = new(factory);

    private static CancellationToken Ct => IdentityScenario.Ct;

    private static object ProfileBody(string fullName = "اسم جديد", string phoneNumber = "0591234567", string city = "جنين", string? bio = "نبذة قصيرة")
        => new { fullName, phoneNumber, city, bio };

    private static async Task<string> UploadPhotoAsync(HttpClient client)
    {
        using var form = Scenario.ImageForm();
        var response = await client.PutAsync(PhotoRoute, form, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("profileImageUrl").GetString()!;
    }

    [Fact]
    public async Task GetMe_Authenticated_ReturnsProfile()
    {
        var user = await _identity.NewUserAsync();

        var me = await IdentityScenario.MeAsync(user.Client);

        Assert.Equal(user.Id, me.GetProperty("id").GetGuid());
        Assert.Equal(user.Email, me.GetProperty("email").GetString());
        Assert.Equal(IdentityScenario.FullName, me.GetProperty("fullName").GetString());
        Assert.Equal("نابلس", me.GetProperty("city").GetString());
        Assert.StartsWith("059", me.GetProperty("phoneNumber").GetString());
        Assert.Equal(new[] { "User" }, me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()!));
        Assert.True(me.GetProperty("hasPassword").GetBoolean());
        Assert.Null(me.OptionalString("profileImageUrl"));
        Assert.Null(me.OptionalString("bio"));
        Assert.True(me.GetProperty("createdAtUtc").GetDateTimeOffset() <= DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _identity.Anonymous().GetAsync(MeRoute, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_InvalidToken_Returns401()
    {
        var response = await _identity.Authorized("not-a-jwt").GetAsync(MeRoute, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_ValidBody_Returns200AndPersistsTrimmedValues()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.PutJsonAsync(MeRoute, ProfileBody("  اسم جديد  ", "0591234567", " جنين ", " نبذة قصيرة "), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadJsonAsync();
        Assert.Equal("اسم جديد", updated.GetProperty("fullName").GetString());
        var me = await IdentityScenario.MeAsync(user.Client);
        Assert.Equal("اسم جديد", me.GetProperty("fullName").GetString());
        Assert.Equal("0591234567", me.GetProperty("phoneNumber").GetString());
        Assert.Equal("جنين", me.GetProperty("city").GetString());
        Assert.Equal("نبذة قصيرة", me.OptionalString("bio"));
        Assert.Equal(user.Email, me.GetProperty("email").GetString());
    }

    [Fact]
    public async Task UpdateMe_WhitespaceBio_ClearsBio()
    {
        var user = await _identity.NewUserAsync();
        var withBio = await user.Client.PutJsonAsync(MeRoute, ProfileBody(), Ct);
        Assert.Equal(HttpStatusCode.OK, withBio.StatusCode);

        var response = await user.Client.PutJsonAsync(MeRoute, ProfileBody(bio: "   "), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await IdentityScenario.MeAsync(user.Client);
        Assert.Null(me.OptionalString("bio"));
    }

    [Fact]
    public async Task UpdateMe_InvalidFields_Returns400WithFieldKeys()
    {
        var user = await _identity.NewUserAsync();
        var body = ProfileBody(fullName: new string('a', 151), phoneNumber: "123", city: new string('b', 101), bio: new string('c', 1001));

        var response = await user.Client.PutJsonAsync(MeRoute, body, Ct);

        await response.AssertValidationKeysAsync("FullName", "PhoneNumber", "City", "Bio");
    }

    [Fact]
    public async Task UpdateMe_WithoutToken_Returns401()
    {
        var response = await _identity.Anonymous().PutJsonAsync(MeRoute, ProfileBody(), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_ValidImage_Returns200AndSetsProfileImage()
    {
        var user = await _identity.NewUserAsync();

        var url = await UploadPhotoAsync(user.Client);

        Assert.StartsWith($"https://files.test/judhur/users/{user.Id}/", url);
        Assert.Contains(IdentityScenario.PublicIdOf(url), _identity.Factory.Files.Files);
        var me = await IdentityScenario.MeAsync(user.Client);
        Assert.Equal(url, me.OptionalString("profileImageUrl"));
    }

    [Fact]
    public async Task UploadPhoto_ReplacingImage_DeletesPreviousImage()
    {
        var user = await _identity.NewUserAsync();
        var previous = await UploadPhotoAsync(user.Client);

        var current = await UploadPhotoAsync(user.Client);

        Assert.NotEqual(previous, current);
        Assert.Contains(IdentityScenario.PublicIdOf(previous), _identity.Factory.Files.Deleted);
        var me = await IdentityScenario.MeAsync(user.Client);
        Assert.Equal(current, me.OptionalString("profileImageUrl"));
    }

    [Fact]
    public async Task UploadPhoto_UnsupportedContentType_Returns400ContentType()
    {
        var user = await _identity.NewUserAsync();
        using var form = Scenario.ImageForm(contentType: "image/gif", fileName: "photo.gif");

        var response = await user.Client.PutAsync(PhotoRoute, form, Ct);

        await response.AssertValidationKeysAsync("ContentType");
    }

    [Fact]
    public async Task UploadPhoto_ContentNotMatchingImage_Returns400InvalidFileContent()
    {
        var user = await _identity.NewUserAsync();
        using var form = Scenario.ImageForm(content: Encoding.ASCII.GetBytes(FakeFileStorage.InvalidContentMarker));

        var response = await user.Client.PutAsync(PhotoRoute, form, Ct);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Storage.InvalidFileContent");
        var me = await IdentityScenario.MeAsync(user.Client);
        Assert.Null(me.OptionalString("profileImageUrl"));
    }

    [Fact]
    public async Task UploadPhoto_WithoutToken_Returns401()
    {
        using var form = Scenario.ImageForm();

        var response = await _identity.Anonymous().PutAsync(PhotoRoute, form, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemovePhoto_WithImage_Returns204AndDeletesImage()
    {
        var user = await _identity.NewUserAsync();
        var url = await UploadPhotoAsync(user.Client);

        var response = await user.Client.DeleteAsync(PhotoRoute, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(IdentityScenario.PublicIdOf(url), _identity.Factory.Files.Deleted);
        var me = await IdentityScenario.MeAsync(user.Client);
        Assert.Null(me.OptionalString("profileImageUrl"));
    }

    [Fact]
    public async Task RemovePhoto_WithoutImage_Returns204()
    {
        var user = await _identity.NewUserAsync();

        var response = await user.Client.DeleteAsync(PhotoRoute, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemovePhoto_WithoutToken_Returns401()
    {
        var response = await _identity.Anonymous().DeleteAsync(PhotoRoute, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
