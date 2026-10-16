using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

using static Judhur.Api.IntegrationTests.Social.SocialApi;

namespace Judhur.Api.IntegrationTests.Social;

[Collection(ApiCollection.Name)]
public sealed class ReviewsTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Status_WithoutConversation_CannotReview()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");

        var status = await ReviewStatusAsync(buyer.Client, seller.Id);

        Assert.False(status.GetProperty("canReview").GetBoolean());
        Assert.True(IsMissing(status, "review"));
    }

    [Fact]
    public async Task Create_SellerNeverReplied_Returns409NotEligible()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        var conversationId = await StartConversationAsync(buyer.Client, listing.Id);
        await SendMessageAsync(buyer.Client, conversationId, "رسالة ثانية بدون رد");

        var status = await ReviewStatusAsync(buyer.Client, seller.Id);
        var response = await ReviewAsync(buyer.Client, seller.Id, 4m);

        Assert.False(status.GetProperty("canReview").GetBoolean());
        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ReviewErrors.NotEligible");
    }

    [Fact]
    public async Task Create_SellerContactedBuyerAsBuyer_DoesNotMakeBuyerEligible()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var buyersListing = await PublishedListingAsync(_scenario, buyer);
        await EligibleConversationAsync(seller, buyer, buyersListing.Id);

        var response = await ReviewAsync(buyer.Client, seller.Id, 4m);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ReviewErrors.NotEligible");
    }

    [Fact]
    public async Task Create_AfterSellerReplied_Returns201AndUpdatesStatusSellerPageAndPublicList()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer", "مشتري مقيّم");
        var listing = await PublishedListingAsync(_scenario, seller);
        await EligibleConversationAsync(buyer, seller, listing.Id);
        var before = await SellerPageAsync(_scenario.Anonymous(), seller.Id);
        var eligible = await ReviewStatusAsync(buyer.Client, seller.Id);

        var response = await ReviewAsync(buyer.Client, seller.Id, 4.5m, "  بائع صادق  ");

        Assert.True(eligible.GetProperty("canReview").GetBoolean());
        Assert.True(IsMissing(eligible, "review"));
        Assert.True(IsMissing(before, "averageRating"));
        Assert.Equal(0, before.GetProperty("reviewsCount").GetInt32());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var review = await response.ReadJsonAsync();
        var reviewId = review.GetProperty("id").GetGuid();
        Assert.Equal(seller.Id, review.GetProperty("sellerId").GetGuid());
        Assert.Equal(4.5m, review.GetProperty("rating").GetDecimal());
        Assert.Equal("بائع صادق", Text(review, "comment"));
        var status = await ReviewStatusAsync(buyer.Client, seller.Id);
        Assert.False(status.GetProperty("canReview").GetBoolean());
        Assert.Equal(reviewId, status.GetProperty("review").GetProperty("id").GetGuid());
        var page = await SellerPageAsync(_scenario.Anonymous(), seller.Id);
        Assert.Equal(4.5m, page.GetProperty("averageRating").GetDecimal());
        Assert.Equal(1, page.GetProperty("reviewsCount").GetInt32());
        var item = await SingleAsync(_scenario.Anonymous(), $"{Routes.Sellers}/{seller.Id}/reviews", reviewId);
        Assert.Equal(4.5m, item.GetProperty("rating").GetDecimal());
        Assert.Equal("بائع صادق", Text(item, "comment"));
        Assert.False(item.GetProperty("isEdited").GetBoolean());
        var reviewer = item.GetProperty("reviewer");
        Assert.Equal("مشتري مقيّم", Text(reviewer, "fullName"));
        Assert.False(reviewer.TryGetProperty("id", out _));
        Assert.False(reviewer.TryGetProperty("phoneNumber", out _));
        var details = await GetJsonAsync(_scenario.Anonymous(), $"{Routes.Properties}/{listing.Id}");
        Assert.Equal(4.5m, details.GetProperty("sellerRating").GetProperty("averageRating").GetDecimal());
        Assert.Equal(1, details.GetProperty("sellerRating").GetProperty("reviewsCount").GetInt32());
    }

    [Fact]
    public async Task Create_NotifiesSellerWithNewReview()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        await EligibleConversationAsync(buyer, seller, listing.Id);

        await CreateReviewAsync(buyer.Client, seller.Id, 3.5m);

        var notification = await WaitForNotificationAsync(seller.Client, "NewReview", seller.Id);
        Assert.Equal("حصلت على تقييم جديد", Text(notification, "title"));
        Assert.Contains("3.5", Text(notification, "body"));
        Assert.Empty(await NotificationsAsync(buyer.Client, "NewReview"));
    }

    [Fact]
    public async Task Create_SecondTime_Returns409DuplicateReview()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        await EligibleConversationAsync(buyer, seller, listing.Id);
        await CreateReviewAsync(buyer.Client, seller.Id, 5m);

        var response = await ReviewAsync(buyer.Client, seller.Id, 1m);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ReviewErrors.DuplicateReview");
        Assert.Equal(5m, (await SellerPageAsync(_scenario.Anonymous(), seller.Id)).GetProperty("averageRating").GetDecimal());
    }

    [Fact]
    public async Task Create_ForSelf_Returns409CannotReviewSelf()
    {
        var user = await _scenario.NewUserAsync("user");

        var response = await ReviewAsync(user.Client, user.Id, 5m);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ReviewErrors.CannotReviewSelf");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4.3)]
    [InlineData(5.5)]
    public async Task Create_InvalidRating_Returns400(double rating)
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");

        var response = await ReviewAsync(buyer.Client, seller.Id, (decimal)rating);

        await AssertValidationErrorAsync(response, "Rating");
    }

    [Fact]
    public async Task Create_CommentTooLong_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");

        var response = await ReviewAsync(buyer.Client, seller.Id, 4m, new string('ت', 501));

        await AssertValidationErrorAsync(response, "Comment");
    }

    [Fact]
    public async Task Create_BannedSeller_Returns404SellerNotFound()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        await EligibleConversationAsync(buyer, seller, listing.Id);
        await BanAsync(admin.Client, seller.Id);

        var response = await ReviewAsync(buyer.Client, seller.Id, 4m);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Seller.NotFound");
        Assert.False((await ReviewStatusAsync(buyer.Client, seller.Id)).GetProperty("canReview").GetBoolean());
        await AssertProblemAsync(await _scenario.Anonymous().GetAsync($"{Routes.Sellers}/{seller.Id}/reviews", Ct), HttpStatusCode.NotFound, "Seller.NotFound");
    }

    [Fact]
    public async Task Update_OwnReview_ChangesRatingAndMarksEdited()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        await EligibleConversationAsync(buyer, seller, listing.Id);
        var reviewId = (await CreateReviewAsync(buyer.Client, seller.Id, 5m)).GetProperty("id").GetGuid();
        Assert.Equal(5m, (await SellerPageAsync(_scenario.Anonymous(), seller.Id)).GetProperty("averageRating").GetDecimal());

        var response = await buyer.Client.PutJsonAsync($"{Routes.Reviews}/{reviewId}", new { rating = 2m, comment = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadJsonAsync();
        Assert.Equal(reviewId, updated.GetProperty("id").GetGuid());
        Assert.Equal(2m, updated.GetProperty("rating").GetDecimal());
        Assert.True(IsMissing(updated, "comment"));
        var page = await SellerPageAsync(_scenario.Anonymous(), seller.Id);
        Assert.Equal(2m, page.GetProperty("averageRating").GetDecimal());
        Assert.Equal(1, page.GetProperty("reviewsCount").GetInt32());
        var item = await SingleAsync(_scenario.Anonymous(), $"{Routes.Sellers}/{seller.Id}/reviews", reviewId);
        Assert.True(item.GetProperty("isEdited").GetBoolean());
        Assert.Equal(2m, item.GetProperty("rating").GetDecimal());
    }

    [Fact]
    public async Task Update_InvalidRating_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        await EligibleConversationAsync(buyer, seller, listing.Id);
        var reviewId = (await CreateReviewAsync(buyer.Client, seller.Id, 5m)).GetProperty("id").GetGuid();

        var response = await buyer.Client.PutJsonAsync($"{Routes.Reviews}/{reviewId}", new { rating = 0.25m, comment = "x" });

        await AssertValidationErrorAsync(response, "Rating");
    }

    [Fact]
    public async Task UpdateOrDelete_SomeoneElsesReview_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var intruder = await _scenario.NewUserAsync("intruder");
        var listing = await PublishedListingAsync(_scenario, seller);
        await EligibleConversationAsync(buyer, seller, listing.Id);
        var reviewId = (await CreateReviewAsync(buyer.Client, seller.Id, 5m)).GetProperty("id").GetGuid();

        var update = await intruder.Client.PutJsonAsync($"{Routes.Reviews}/{reviewId}", new { rating = 1m, comment = "x" });
        var delete = await intruder.Client.DeleteAsync($"{Routes.Reviews}/{reviewId}", Ct);
        var sellerDelete = await seller.Client.DeleteAsync($"{Routes.Reviews}/{reviewId}", Ct);

        await AssertProblemAsync(update, HttpStatusCode.NotFound, "ReviewErrors.NotFound");
        await AssertProblemAsync(delete, HttpStatusCode.NotFound, "ReviewErrors.NotFound");
        await AssertProblemAsync(sellerDelete, HttpStatusCode.NotFound, "ReviewErrors.NotFound");
        Assert.Equal(5m, (await SellerPageAsync(_scenario.Anonymous(), seller.Id)).GetProperty("averageRating").GetDecimal());
    }

    [Fact]
    public async Task Delete_OwnReview_RemovesItAndAllowsReviewingAgain()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        await EligibleConversationAsync(buyer, seller, listing.Id);
        var reviewId = (await CreateReviewAsync(buyer.Client, seller.Id, 1m)).GetProperty("id").GetGuid();
        Assert.Equal(1, (await SellerPageAsync(_scenario.Anonymous(), seller.Id)).GetProperty("reviewsCount").GetInt32());

        var response = await buyer.Client.DeleteAsync($"{Routes.Reviews}/{reviewId}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var page = await SellerPageAsync(_scenario.Anonymous(), seller.Id);
        Assert.True(IsMissing(page, "averageRating"));
        Assert.Equal(0, page.GetProperty("reviewsCount").GetInt32());
        Assert.Null(await FindAsync(_scenario.Anonymous(), $"{Routes.Sellers}/{seller.Id}/reviews", reviewId));
        var status = await ReviewStatusAsync(buyer.Client, seller.Id);
        Assert.True(status.GetProperty("canReview").GetBoolean());
        Assert.True(IsMissing(status, "review"));
        Assert.Equal(HttpStatusCode.Created, (await ReviewAsync(buyer.Client, seller.Id, 4m)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await buyer.Client.DeleteAsync($"{Routes.Reviews}/{reviewId}", Ct)).StatusCode);
    }

    [Fact]
    public async Task SellerPage_SeveralReviews_ShowsRoundedAverageCountAndNewestFirst()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var first = await _scenario.NewUserAsync("buyer");
        var second = await _scenario.NewUserAsync("buyer");
        var third = await _scenario.NewUserAsync("buyer");
        var listing = await PublishedListingAsync(_scenario, seller);
        await EligibleConversationAsync(first, seller, listing.Id);
        await EligibleConversationAsync(second, seller, listing.Id);
        await EligibleConversationAsync(third, seller, listing.Id);
        var firstReview = (await CreateReviewAsync(first.Client, seller.Id, 5m)).GetProperty("id").GetGuid();
        var secondReview = (await CreateReviewAsync(second.Client, seller.Id, 4.5m)).GetProperty("id").GetGuid();
        var thirdReview = (await CreateReviewAsync(third.Client, seller.Id, 4m, null)).GetProperty("id").GetGuid();

        var page = await SellerPageAsync(_scenario.Anonymous(), seller.Id);
        var reviews = await GetJsonAsync(_scenario.Anonymous(), $"{Routes.Sellers}/{seller.Id}/reviews");

        Assert.Equal(4.5m, page.GetProperty("averageRating").GetDecimal());
        Assert.Equal(3, page.GetProperty("reviewsCount").GetInt32());
        Assert.Equal(1, page.GetProperty("activeListingsCount").GetInt32());
        Assert.Equal(3, reviews.GetProperty("totalCount").GetInt32());
        var ids = Scenario.Items(reviews).EnumerateArray().Select(r => r.GetProperty("id").GetGuid()).ToList();
        Assert.Equal([thirdReview, secondReview, firstReview], ids);
        Assert.True(IsMissing(Scenario.Items(reviews)[0], "comment"));
    }

    [Fact]
    public async Task SellerReviews_InvalidPageSize_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");

        var response = await _scenario.Anonymous().GetAsync($"{Routes.Sellers}/{seller.Id}/reviews?pageSize=101", Ct);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pagination.PageSizeInvalid");
    }

    [Fact]
    public async Task SellerPage_AdminAccount_Returns404()
    {
        var admin = await _scenario.AdminAsync();

        var response = await _scenario.Anonymous().GetAsync($"{Routes.Sellers}/{admin.Id}", Ct);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Seller.NotFound");
    }

    [Fact]
    public async Task Endpoints_Anonymous_Returns401()
    {
        var anonymous = _scenario.Anonymous();

        var status = await anonymous.GetAsync($"{Routes.Reviews}/sellers/{Guid.NewGuid()}/mine", Ct);
        var create = await ReviewAsync(anonymous, Guid.NewGuid(), 4m);

        Assert.Equal(HttpStatusCode.Unauthorized, status.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }
}
