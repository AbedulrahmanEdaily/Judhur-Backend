using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

using static Judhur.Api.IntegrationTests.Social.SocialApi;

namespace Judhur.Api.IntegrationTests.EndToEnd;

[Collection(ApiCollection.Name)]
public sealed class ListingToReviewJourneyTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Journey_SellerPublishesBuyerChatsAndReviews_SellerPageShowsRating()
    {
        var anonymous = _scenario.Anonymous();
        var email = Scenario.NewEmail("journey-seller");
        var register = await _scenario.RegisterAsync(email, fullName: "بائع الرحلة");
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        await AssertProblemAsync(await LoginAsync(_scenario, email), HttpStatusCode.Forbidden, "Identity.EmailNotConfirmed");
        var (sellerId, confirmationToken) = await _scenario.ReadConfirmationAsync(email);
        var confirm = await anonymous.PostJsonAsync($"{Routes.Account}/confirm-email", new { userId = sellerId, token = confirmationToken });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        var tokens = await _scenario.LoginAsync(email);
        var seller = new TestUser(sellerId, email, Scenario.DefaultPassword, "بائع الرحلة", tokens, TestUser.Authorize(factory.CreateClient(), tokens.AccessToken));
        Assert.Equal(sellerId, (await GetJsonAsync(seller.Client, $"{Routes.Account}/me")).GetProperty("id").GetGuid());

        var token = NewToken();
        var title = TitleFor(token);
        var propertyId = await Scenario.CreatePropertyAsync(seller.Client, ListingBody(token));
        var draft = await MyListingAsync(seller.Client, propertyId);
        Assert.Equal(title, Text(draft, "title"));
        Assert.Equal("Pending", Text(draft, "moderationStatus"));
        Assert.False(draft.GetProperty("hasOwnershipDocument").GetBoolean());
        Assert.Equal(0, draft.GetProperty("images").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, await PublicDetailsStatusAsync(anonymous, propertyId));

        var mainImage = await Scenario.AddImageAsync(seller.Client, propertyId);
        await Scenario.AddImageAsync(seller.Client, propertyId);
        await Scenario.AddImageAsync(seller.Client, propertyId);
        await Scenario.UploadDocumentAsync(seller.Client, propertyId);
        var ready = await MyListingAsync(seller.Client, propertyId);
        Assert.Equal(3, ready.GetProperty("images").GetArrayLength());
        Assert.Equal(mainImage, ready.GetProperty("images").EnumerateArray().Single(i => i.GetProperty("isMainImage").GetBoolean()).GetProperty("id").GetGuid());
        Assert.True(ready.GetProperty("hasOwnershipDocument").GetBoolean());
        Assert.Equal("Pending", Text(ready, "moderationStatus"));

        var admin = await _scenario.AdminAsync();
        var queued = await SingleAsync(admin.Client, $"{Routes.AdminProperties}/pending", propertyId);
        Assert.Equal(title, Text(queued, "title"));
        var queuedNotification = await WaitForNotificationAsync(admin.Client, "PropertyPendingReview", propertyId);
        Assert.Contains(title, Text(queuedNotification, "body"));
        var forReview = await GetJsonAsync(admin.Client, $"{Routes.AdminProperties}/{propertyId}");
        Assert.Equal(sellerId, forReview.GetProperty("seller").GetProperty("id").GetGuid());
        Assert.False(IsMissing(forReview, "ownershipDocumentUrl"));
        await Scenario.ApproveAsync(admin.Client, propertyId);
        Assert.Null(await FindAsync(admin.Client, $"{Routes.AdminProperties}/pending", propertyId));
        var approved = await WaitForNotificationAsync(seller.Client, "PropertyApproved", propertyId);
        Assert.Contains(title, Text(approved, "body"));
        Assert.Equal("Approved", Text(await MyListingAsync(seller.Client, propertyId), "moderationStatus"));

        var buyer = await _scenario.NewUserAsync("journey-buyer", "مشتري الرحلة");
        Assert.True(await IsInSearchAsync(buyer.Client, token));
        Assert.True(await IsInSearchAsync(anonymous, token));
        var details = await GetJsonAsync(buyer.Client, $"{Routes.Properties}/{propertyId}");
        Assert.Equal(title, Text(details, "title"));
        Assert.Equal(sellerId, details.GetProperty("user").GetProperty("id").GetGuid());
        Assert.False(IsMissing(details.GetProperty("user"), "phoneNumber"));
        Assert.Equal(3, details.GetProperty("images").GetArrayLength());

        var favorite = await buyer.Client.PostAsync($"{Routes.Favorites}/{propertyId}", null, Ct);
        Assert.Equal(HttpStatusCode.NoContent, favorite.StatusCode);
        var favoriteIds = await (await buyer.Client.GetAsync($"{Routes.Favorites}/ids", Ct)).ReadAsync<List<Guid>>();
        Assert.Contains(propertyId, favoriteIds);
        Assert.NotNull(await FindAsync(buyer.Client, Routes.Favorites, propertyId));

        var start = await StartAsync(buyer.Client, propertyId, "مرحبا، هل يمكنني معاينة الشقة؟");
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);
        var conversationId = (await start.ReadJsonAsync()).GetProperty("conversationId").GetGuid();
        Assert.Equal(1, await UnreadMessagesAsync(seller.Client));
        var sellerInbox = await InboxItemAsync(seller.Client, conversationId);
        Assert.Equal("Seller", Text(sellerInbox, "myRole"));
        Assert.Equal("مشتري الرحلة", Text(sellerInbox.GetProperty("otherParty"), "fullName"));
        Assert.Equal(1, sellerInbox.GetProperty("unreadCount").GetInt32());
        Assert.False((await ReviewStatusAsync(buyer.Client, sellerId)).GetProperty("canReview").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Conversations}/{conversationId}/read")).StatusCode);
        Assert.Equal(0, await UnreadMessagesAsync(seller.Client));
        var reply = await SendMessageAsync(seller.Client, conversationId, "أهلًا، المعاينة متاحة غدًا");
        Assert.Equal(sellerId, reply.GetProperty("senderId").GetGuid());
        Assert.Equal(1, await UnreadMessagesAsync(buyer.Client));
        var buyerInbox = await InboxItemAsync(buyer.Client, conversationId);
        Assert.Equal("أهلًا، المعاينة متاحة غدًا", Text(buyerInbox, "lastMessagePreview"));
        Assert.Equal(1, buyerInbox.GetProperty("unreadCount").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(buyer.Client, $"{Routes.Conversations}/{conversationId}/read")).StatusCode);
        Assert.Equal(0, await UnreadMessagesAsync(buyer.Client));

        Assert.True((await ReviewStatusAsync(buyer.Client, sellerId)).GetProperty("canReview").GetBoolean());
        var review = await CreateReviewAsync(buyer.Client, sellerId, 5m, "بائع متعاون وصادق");
        var reviewId = review.GetProperty("id").GetGuid();
        var newReview = await WaitForNotificationAsync(seller.Client, "NewReview", sellerId);
        Assert.Contains("5", Text(newReview, "body"));

        var page = await SellerPageAsync(anonymous, sellerId);
        Assert.Equal("بائع الرحلة", Text(page, "fullName"));
        Assert.Equal(5m, page.GetProperty("averageRating").GetDecimal());
        Assert.Equal(1, page.GetProperty("reviewsCount").GetInt32());
        Assert.Equal(1, page.GetProperty("activeListingsCount").GetInt32());
        Assert.False(page.TryGetProperty("email", out _));
        Assert.False(page.TryGetProperty("phoneNumber", out _));
        var publicReview = await SingleAsync(anonymous, $"{Routes.Sellers}/{sellerId}/reviews", reviewId);
        Assert.Equal("مشتري الرحلة", Text(publicReview.GetProperty("reviewer"), "fullName"));
        Assert.Equal("بائع متعاون وصادق", Text(publicReview, "comment"));
        var guestDetails = await GetJsonAsync(anonymous, $"{Routes.Properties}/{propertyId}");
        Assert.Equal(5m, guestDetails.GetProperty("sellerRating").GetProperty("averageRating").GetDecimal());
        Assert.True(IsMissing(guestDetails.GetProperty("user"), "phoneNumber"));
        var sellerListings = await GetJsonAsync(anonymous, $"{Routes.Properties}?sellerId={sellerId}");
        Assert.Equal([propertyId], Scenario.Items(sellerListings).EnumerateArray().Select(p => p.GetProperty("id").GetGuid()));
    }
}
