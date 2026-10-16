using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

using static Judhur.Api.IntegrationTests.Social.SocialApi;

namespace Judhur.Api.IntegrationTests.Social;

[Collection(ApiCollection.Name)]
public sealed class ConversationsTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Start_PublishedListing_Returns201ThenAgainReturns200WithSameConversation()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);

        var first = await StartAsync(buyer.Client, listing.Id, "  هل السعر قابل للتفاوض؟  ");
        var second = await StartAsync(buyer.Client, listing.Id, "ما زلت أنتظر ردك");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await first.ReadJsonAsync();
        Assert.True(created.GetProperty("isNew").GetBoolean());
        var conversationId = created.GetProperty("conversationId").GetGuid();
        var message = created.GetProperty("message");
        Assert.Equal(conversationId, message.GetProperty("conversationId").GetGuid());
        Assert.Equal(buyer.Id, message.GetProperty("senderId").GetGuid());
        Assert.Equal("هل السعر قابل للتفاوض؟", Text(message, "content"));
        Assert.True(message.GetProperty("isMine").GetBoolean());
        Assert.True(IsMissing(message, "readAtUtc"));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var existing = await second.ReadJsonAsync();
        Assert.False(existing.GetProperty("isNew").GetBoolean());
        Assert.Equal(conversationId, existing.GetProperty("conversationId").GetGuid());
        var history = await GetJsonAsync(buyer.Client, $"{Routes.Conversations}/{conversationId}/messages");
        Assert.Equal(2, history.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Start_OwnListing_Returns409CannotStartConversationWithSelf()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var listing = await PublishedListingAsync(_scenario, seller);

        var response = await StartAsync(seller.Client, listing.Id, "رسالة لنفسي");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ConversationErrors.CannotStartConversationWithSelf");
    }

    [Fact]
    public async Task Start_ListingNotPublished_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await ReadyListingAsync(seller);

        var response = await StartAsync(buyer.Client, listing.Id, "مرحبا");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task Start_EmptyMessage_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);

        var empty = await StartAsync(buyer.Client, listing.Id, "   ");
        var tooLong = await StartAsync(buyer.Client, listing.Id, new string('ر', 2001));

        await AssertValidationErrorAsync(empty, "Message");
        await AssertValidationErrorAsync(tooLong, "Message");
        Assert.Equal(0, (await GetJsonAsync(buyer.Client, Routes.Conversations)).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Start_Anonymous_Returns401()
    {
        var response = await StartAsync(_scenario.Anonymous(), Guid.NewGuid(), "مرحبا");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_BothParticipants_Returns201WithMessage()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);

        var reply = await SendAsync(seller.Client, conversationId, "نعم، متاح للمعاينة يوم السبت");
        var answer = await SendAsync(buyer.Client, conversationId, "ممتاز، أراك السبت");

        Assert.Equal(HttpStatusCode.Created, reply.StatusCode);
        var replyBody = await reply.ReadJsonAsync();
        Assert.Equal(conversationId, replyBody.GetProperty("conversationId").GetGuid());
        Assert.Equal(seller.Id, replyBody.GetProperty("senderId").GetGuid());
        Assert.Equal("نعم، متاح للمعاينة يوم السبت", Text(replyBody, "content"));
        Assert.True(replyBody.GetProperty("isMine").GetBoolean());
        Assert.Equal(HttpStatusCode.Created, answer.StatusCode);
        var history = await GetJsonAsync(seller.Client, $"{Routes.Conversations}/{conversationId}/messages");
        var items = history.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal("ممتاز، أراك السبت", Text(items[0], "content"));
        Assert.False(items[0].GetProperty("isMine").GetBoolean());
        Assert.True(items[1].GetProperty("isMine").GetBoolean());
        Assert.False(history.GetProperty("hasMore").GetBoolean());
    }

    [Fact]
    public async Task SendMessage_EmptyContent_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);

        var response = await SendAsync(buyer.Client, conversationId, "");

        await AssertValidationErrorAsync(response, "Content");
    }

    [Fact]
    public async Task SendMessage_AfterListingHidden_StillWorksAndInboxMarksPropertyUnavailable()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{listing.Id}/deactivate")).StatusCode);

        var response = await SendAsync(seller.Client, conversationId, "تم بيع العقار تقريبًا");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await InboxItemAsync(buyer.Client, conversationId);
        Assert.False(item.GetProperty("property").GetProperty("isAvailable").GetBoolean());
        Assert.Equal(listing.Title, Text(item.GetProperty("property"), "title"));
    }

    [Fact]
    public async Task Inbox_BothSides_ShowRoleOtherPartyPreviewAndUnreadCounts()
    {
        var seller = await _scenario.NewUserAsync("seller", "بائع المحادثة");
        var buyer = await _scenario.NewUserAsync("buyer", "مشتري المحادثة");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id, "الرسالة الأولى");
        await SendMessageAsync(buyer.Client, conversationId, "الرسالة الثانية");

        var sellerItem = await InboxItemAsync(seller.Client, conversationId);
        var buyerItem = await InboxItemAsync(buyer.Client, conversationId);

        Assert.Equal("Seller", Text(sellerItem, "myRole"));
        Assert.Equal(buyer.Id, sellerItem.GetProperty("otherParty").GetProperty("id").GetGuid());
        Assert.Equal("مشتري المحادثة", Text(sellerItem.GetProperty("otherParty"), "fullName"));
        Assert.Equal(2, sellerItem.GetProperty("unreadCount").GetInt32());
        Assert.Equal("الرسالة الثانية", Text(sellerItem, "lastMessagePreview"));
        Assert.False(sellerItem.GetProperty("lastMessageIsMine").GetBoolean());
        Assert.False(IsMissing(sellerItem, "lastMessageAtUtc"));
        var property = sellerItem.GetProperty("property");
        Assert.Equal(listing.Id, property.GetProperty("id").GetGuid());
        Assert.Equal(listing.Title, Text(property, "title"));
        Assert.True(property.GetProperty("isAvailable").GetBoolean());
        Assert.False(IsMissing(property, "mainImageUrl"));
        Assert.Equal("Buyer", Text(buyerItem, "myRole"));
        Assert.Equal(seller.Id, buyerItem.GetProperty("otherParty").GetProperty("id").GetGuid());
        Assert.Equal("بائع المحادثة", Text(buyerItem.GetProperty("otherParty"), "fullName"));
        Assert.Equal(0, buyerItem.GetProperty("unreadCount").GetInt32());
        Assert.True(buyerItem.GetProperty("lastMessageIsMine").GetBoolean());
        Assert.Equal(2, await UnreadMessagesAsync(seller.Client));
        Assert.Equal(0, await UnreadMessagesAsync(buyer.Client));
    }

    [Fact]
    public async Task Inbox_LongLastMessage_IsTruncatedToPreview()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id, new string('ع', 150));

        var item = await InboxItemAsync(seller.Client, conversationId);

        Assert.Equal(new string('ع', 100) + "…", Text(item, "lastMessagePreview"));
    }

    [Fact]
    public async Task Inbox_MostRecentActivityFirst()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var older = await PublishedListingAsync(_scenario, seller);
        var newer = await PublishedListingAsync(_scenario, seller);
        var olderConversation = await StartConversationAsync(buyer.Client, older.Id);
        var newerConversation = await StartConversationAsync(buyer.Client, newer.Id);
        await SendMessageAsync(buyer.Client, olderConversation, "تذكير");

        var inbox = await GetJsonAsync(seller.Client, Routes.Conversations);

        Assert.Equal(2, inbox.GetProperty("totalCount").GetInt32());
        var ids = Scenario.Items(inbox).EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToList();
        Assert.Equal([olderConversation, newerConversation], ids);
    }

    [Fact]
    public async Task MarkRead_Recipient_ClearsUnreadAndStampsMessages()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        await SendMessageAsync(buyer.Client, conversationId, "هل يمكنني زيارة العقار؟");
        await SendMessageAsync(seller.Client, conversationId, "أهلا بك");

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Conversations}/{conversationId}/read");
        var again = await PostEmptyAsync(seller.Client, $"{Routes.Conversations}/{conversationId}/read");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Equal(0, await UnreadMessagesAsync(seller.Client));
        Assert.Equal(0, (await InboxItemAsync(seller.Client, conversationId)).GetProperty("unreadCount").GetInt32());
        Assert.Equal(1, await UnreadMessagesAsync(buyer.Client));
        var history = await GetJsonAsync(buyer.Client, $"{Routes.Conversations}/{conversationId}/messages");
        foreach (var message in history.GetProperty("items").EnumerateArray())
        {
            var fromBuyer = message.GetProperty("senderId").GetGuid() == buyer.Id;
            Assert.Equal(fromBuyer, !IsMissing(message, "readAtUtc"));
        }
    }

    [Fact]
    public async Task GetById_Participant_ReturnsHeader()
    {
        var seller = await _scenario.NewUserAsync("seller", "صاحب العقار");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);

        var header = await GetJsonAsync(buyer.Client, $"{Routes.Conversations}/{conversationId}");

        Assert.Equal(conversationId, header.GetProperty("id").GetGuid());
        Assert.Equal("Buyer", Text(header, "myRole"));
        Assert.Equal(seller.Id, header.GetProperty("otherParty").GetProperty("id").GetGuid());
        Assert.Equal("صاحب العقار", Text(header.GetProperty("otherParty"), "fullName"));
        Assert.Equal(listing.Id, header.GetProperty("property").GetProperty("id").GetGuid());
        Assert.True(header.GetProperty("property").GetProperty("isAvailable").GetBoolean());
        Assert.Equal("Seller", Text(await GetJsonAsync(seller.Client, $"{Routes.Conversations}/{conversationId}"), "myRole"));
    }

    [Fact]
    public async Task Messages_CursorPaging_WalksBackThroughHistory()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id, "رسالة 1");
        for (var i = 2; i <= 5; i++)
        {
            await SendMessageAsync(i % 2 == 0 ? seller.Client : buyer.Client, conversationId, $"رسالة {i}");
        }
        var url = $"{Routes.Conversations}/{conversationId}/messages";

        var first = await GetJsonAsync(buyer.Client, $"{url}?limit=2");
        var firstItems = first.GetProperty("items").EnumerateArray().ToList();
        var second = await GetJsonAsync(buyer.Client, $"{url}?limit=2&before={firstItems[^1].GetProperty("id").GetGuid()}");
        var secondItems = second.GetProperty("items").EnumerateArray().ToList();
        var third = await GetJsonAsync(buyer.Client, $"{url}?limit=2&before={secondItems[^1].GetProperty("id").GetGuid()}");
        var thirdItems = third.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(["رسالة 5", "رسالة 4"], firstItems.Select(m => Text(m, "content")));
        Assert.True(first.GetProperty("hasMore").GetBoolean());
        Assert.Equal(["رسالة 3", "رسالة 2"], secondItems.Select(m => Text(m, "content")));
        Assert.True(second.GetProperty("hasMore").GetBoolean());
        Assert.Equal(["رسالة 1"], thirdItems.Select(m => Text(m, "content")));
        Assert.False(third.GetProperty("hasMore").GetBoolean());
        var all = await GetJsonAsync(buyer.Client, url);
        Assert.Equal(5, all.GetProperty("items").GetArrayLength());
        Assert.False(all.GetProperty("hasMore").GetBoolean());
    }

    [Fact]
    public async Task Messages_InvalidLimitOrForeignCursor_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var other = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        var otherConversation = await StartConversationAsync(buyer.Client, other.Id);
        var foreignMessage = (await SendMessageAsync(buyer.Client, otherConversation, "رسالة أخرى")).GetProperty("id").GetGuid();
        var url = $"{Routes.Conversations}/{conversationId}/messages";

        var zero = await buyer.Client.GetAsync($"{url}?limit=0", Ct);
        var tooMany = await buyer.Client.GetAsync($"{url}?limit=101", Ct);
        var foreign = await buyer.Client.GetAsync($"{url}?before={foreignMessage}", Ct);

        await AssertValidationErrorAsync(zero, "Limit");
        await AssertValidationErrorAsync(tooMany, "Limit");
        await AssertProblemAsync(foreign, HttpStatusCode.BadRequest, "ConversationErrors.InvalidCursor");
    }

    [Fact]
    public async Task OtherUsersConversation_EveryEndpoint_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var stranger = await _scenario.NewUserAsync("stranger");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        var url = $"{Routes.Conversations}/{conversationId}";

        var header = await stranger.Client.GetAsync(url, Ct);
        var messages = await stranger.Client.GetAsync($"{url}/messages", Ct);
        var send = await SendAsync(stranger.Client, conversationId, "تطفل");
        var read = await PostEmptyAsync(stranger.Client, $"{url}/read");

        await AssertProblemAsync(header, HttpStatusCode.NotFound, "ConversationErrors.NotFound");
        await AssertProblemAsync(messages, HttpStatusCode.NotFound, "ConversationErrors.NotFound");
        await AssertProblemAsync(send, HttpStatusCode.NotFound, "ConversationErrors.NotFound");
        await AssertProblemAsync(read, HttpStatusCode.NotFound, "ConversationErrors.NotFound");
        Assert.Equal(0, (await GetJsonAsync(stranger.Client, Routes.Conversations)).GetProperty("totalCount").GetInt32());
        Assert.Equal(1, (await GetJsonAsync(buyer.Client, $"{url}/messages")).GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task UnknownConversation_Returns404()
    {
        var user = await _scenario.NewUserAsync("user");

        var response = await user.Client.GetAsync($"{Routes.Conversations}/{Guid.NewGuid()}", Ct);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "ConversationErrors.NotFound");
    }

    [Fact]
    public async Task SendMessage_RecipientBanned_Returns409ParticipantUnavailable()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        await BanAsync(admin.Client, seller.Id);

        var response = await SendAsync(buyer.Client, conversationId, "هل ما زلت هنا؟");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Conversations.ParticipantUnavailable");
        Assert.Equal(1, (await GetJsonAsync(buyer.Client, $"{Routes.Conversations}/{conversationId}/messages")).GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task SendMessage_RecipientDeletedAccount_Returns409ParticipantUnavailable()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAccountAsync(buyer)).StatusCode);

        var response = await SendAsync(seller.Client, conversationId, "العقار متاح");

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Conversations.ParticipantUnavailable");
    }

    [Fact]
    public async Task SendMessage_ToRecipient_RaisesUnreadBadge()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        Assert.Equal(1, await UnreadMessagesAsync(seller.Client));

        await SendMessageAsync(buyer.Client, conversationId, "رسالة إضافية");

        Assert.Equal(2, await UnreadMessagesAsync(seller.Client));
    }

    [Fact]
    public async Task StartConversation_CountsInMessagesBadgeInsteadOfNotifications()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);

        await StartConversationAsync(buyer.Client, listing.Id);

        Assert.Equal(1, await UnreadMessagesAsync(seller.Client));
        Assert.Empty(await NotificationsAsync(seller.Client, "NewMessage"));
    }

    [Fact]
    public async Task MarkAsRead_UpToMessage_LeavesNewerMessagesUnread()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        var seen = await SendMessageAsync(buyer.Client, conversationId, "الرسالة التي قرأها البائع");
        await SendMessageAsync(buyer.Client, conversationId, "رسالة وصلت بعد آخر تحديث");

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Conversations}/{conversationId}/read?upTo={seen.GetProperty("id").GetGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, await UnreadMessagesAsync(seller.Client));
    }

    [Fact]
    public async Task MarkAsRead_UpToMessageOfAnotherConversation_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var other = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        var otherConversationId = await StartConversationAsync(buyer.Client, other.Id);
        var foreign = await SendMessageAsync(buyer.Client, otherConversationId, "رسالة في محادثة أخرى");

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Conversations}/{conversationId}/read?upTo={foreign.GetProperty("id").GetGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
