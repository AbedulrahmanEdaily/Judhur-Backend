using System.Net;
using System.Net.Http.Json;

using Judhur.Api.IntegrationTests.Infrastructure;

using static Judhur.Api.IntegrationTests.Social.SocialApi;

namespace Judhur.Api.IntegrationTests.EndToEnd;

[Collection(ApiCollection.Name)]
public sealed class AccountLifecycleJourneyTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Journey_UserChangesEmailIsBannedUnbannedAndDeletesAccount()
    {
        var admin = await _scenario.AdminAsync();
        var seller = await _scenario.NewUserAsync("journey-seller");
        var listing = await PublishedListingAsync(_scenario, seller);
        var user = await _scenario.NewUserAsync("journey-user", "مستخدم الرحلة");
        var conversationId = await StartConversationAsync(user.Client, listing.Id);

        var newEmail = Scenario.NewEmail("journey-moved");
        var request = await user.Client.PostJsonAsync($"{Routes.Account}/me/email", new { newEmail, password = user.Password });
        Assert.Equal(HttpStatusCode.NoContent, request.StatusCode);
        var link = FakeEmailSender.QueryOf(FakeEmailSender.ExtractLink(await factory.Emails.WaitForAsync(newEmail, Scenario.ConfirmEmailChangeSubject)));
        var confirm = await _scenario.Anonymous().PostJsonAsync($"{Routes.Account}/confirm-email-change", new { userId = user.Id, newEmail = link["email"], token = link["token"] });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await user.Client.GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
        await AssertProblemAsync(await LoginAsync(_scenario, user.Email), HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        var moved = await _scenario.LoginAsync(newEmail);
        var client = TestUser.Authorize(factory.CreateClient(), moved.AccessToken);
        Assert.Equal(newEmail, Text(await GetJsonAsync(client, $"{Routes.Account}/me"), "email"));

        await BanAsync(admin.Client, user.Id);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
        await AssertProblemAsync(await LoginAsync(_scenario, newEmail), HttpStatusCode.Forbidden, "Identity.AccountBanned");
        await AssertProblemAsync(await SendAsync(seller.Client, conversationId, "هل ما زلت مهتمًا؟"), HttpStatusCode.Conflict, "Conversations.ParticipantUnavailable");
        var banned = await SingleAsync(admin.Client, $"{Routes.AdminUsers}?status=Banned&search={Uri.EscapeDataString(newEmail)}", user.Id);
        Assert.Equal(BanReason, Text(banned, "banReason"));

        await UnbanAsync(admin.Client, user.Id);
        var back = await _scenario.LoginAsync(newEmail);
        client = TestUser.Authorize(factory.CreateClient(), back.AccessToken);
        var reply = await SendAsync(seller.Client, conversationId, "أهلًا بعودتك");
        Assert.Equal(HttpStatusCode.Created, reply.StatusCode);

        using (var delete = new HttpRequestMessage(HttpMethod.Delete, $"{Routes.Account}/me") { Content = JsonContent.Create(new { password = user.Password }, options: Json.Options) })
        {
            Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(delete, Ct)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Routes.Account}/me", Ct)).StatusCode);
        await AssertProblemAsync(await LoginAsync(_scenario, newEmail), HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
        var sellerView = await InboxItemAsync(seller.Client, conversationId);
        Assert.Equal(DeletedUserName, Text(sellerView.GetProperty("otherParty"), "fullName"));
        await AssertProblemAsync(await SendAsync(seller.Client, conversationId, "مرحبًا؟"), HttpStatusCode.Conflict, "Conversations.ParticipantUnavailable");
        var register = await _scenario.RegisterAsync(newEmail);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
    }
}
