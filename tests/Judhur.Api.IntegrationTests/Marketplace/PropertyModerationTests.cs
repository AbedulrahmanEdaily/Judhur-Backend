using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Domain.Notifications.Enums;
using Judhur.Domain.Properties.Enums;

using static Judhur.Api.IntegrationTests.Marketplace.Market;

namespace Judhur.Api.IntegrationTests.Marketplace;

[Collection(ApiCollection.Name)]
public sealed class PropertyModerationTests(JudhurApiFactory factory)
{
    private const string UnknownRowVersion = "AAAAAAAAAAA=";

    private readonly Scenario _scenario = new(factory);

    private static string ApproveUrl(Guid propertyId) => $"{Routes.AdminProperties}/{propertyId}/approve";

    private static string RejectUrl(Guid propertyId) => $"{Routes.AdminProperties}/{propertyId}/reject";

    [Fact]
    public async Task Pending_ReadyListing_IsListedWithItsSummary()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var body = new ListingBody { Title = NewTitle("قائمة المراجعة") };
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, body);

        var pending = await FindPendingAsync(admin.Client, id);

        Assert.NotNull(pending);
        Assert.Equal(body.Title, pending.Title);
        Assert.Equal(body.Price, pending.Price);
        Assert.Equal(body.City, pending.City);
        Assert.StartsWith(FilesHost, pending.MainImageUrl);
    }

    [Fact]
    public async Task Pending_ListingsNotReady_AreNotListed()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var noImages = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        await Scenario.UploadDocumentAsync(seller.Client, noImages);
        var twoImages = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        await Scenario.AddImageAsync(seller.Client, twoImages);
        await Scenario.AddImageAsync(seller.Client, twoImages);
        await Scenario.UploadDocumentAsync(seller.Client, twoImages);
        var noDocument = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        for (var i = 0; i < 3; i++)
        {
            await Scenario.AddImageAsync(seller.Client, noDocument);
        }
        var inactive = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{inactive}/deactivate")).StatusCode);

        Assert.Null(await FindPendingAsync(admin.Client, noImages));
        Assert.Null(await FindPendingAsync(admin.Client, twoImages));
        Assert.Null(await FindPendingAsync(admin.Client, noDocument));
        Assert.Null(await FindPendingAsync(admin.Client, inactive));
    }

    [Fact]
    public async Task Pending_ApprovedOrRejectedListings_LeaveTheQueue()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var approved = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var rejected = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        Assert.NotNull(await FindPendingAsync(admin.Client, approved));
        Assert.NotNull(await FindPendingAsync(admin.Client, rejected));

        await Scenario.ApproveAsync(admin.Client, approved);
        await RejectAsync(admin.Client, rejected);

        Assert.Null(await FindPendingAsync(admin.Client, approved));
        Assert.Null(await FindPendingAsync(admin.Client, rejected));
    }

    [Theory]
    [InlineData("page=0", "Pagination.PageInvalid")]
    [InlineData("page=10001", "Pagination.PageInvalid")]
    [InlineData("pageSize=101", "Pagination.PageSizeInvalid")]
    public async Task Pending_PaginationOutOfRange_Returns400(string query, string code)
    {
        var admin = await _scenario.AdminAsync();

        var response = await admin.Client.GetAsync($"{Routes.AdminProperties}/pending?{query}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, code);
    }

    [Fact]
    public async Task Pending_AsUser_Returns403()
    {
        var user = await _scenario.NewUserAsync("user");

        var response = await user.Client.GetAsync($"{Routes.AdminProperties}/pending", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pending_AsGuest_Returns401()
    {
        var response = await _scenario.Anonymous().GetAsync($"{Routes.AdminProperties}/pending", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Review_ReadyListing_ReturnsEverythingNeededToReview()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var body = new ListingBody();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, body);
        var started = DateTimeOffset.UtcNow;

        var review = await GetForReviewAsync(admin.Client, id);

        Assert.Equal(id, review.Id);
        Assert.Equal(body.Title, review.Title);
        Assert.Equal(body.FullAddress, review.FullAddress);
        Assert.Equal(ModerationStatus.Pending, review.ModerationStatus);
        Assert.Equal(3, review.Images.Count);
        Assert.Single(review.Images, i => i.IsMainImage);
        Assert.NotNull(review.Seller);
        Assert.Equal(seller.Id, review.Seller.Id);
        Assert.Equal(8, Convert.FromBase64String(review.RowVersion).Length);
        Assert.NotNull(review.OwnershipDocumentUrl);
        Assert.StartsWith($"{PrivateFilesPrefix}judhur/ownership-documents/{id}/", review.OwnershipDocumentUrl, StringComparison.Ordinal);
        Assert.NotNull(review.OwnershipDocumentExpiresAtUtc);
        Assert.InRange(review.OwnershipDocumentExpiresAtUtc.Value, started.AddMinutes(9), started.AddMinutes(11));
    }

    [Fact]
    public async Task Review_ListingWithoutDocument_HasNoDocumentLink()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await admin.Client.GetAsync($"{Routes.AdminProperties}/{id}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.True(IsNullOrMissing(json, "ownershipDocumentUrl"));
        Assert.True(IsNullOrMissing(json, "ownershipDocumentExpiresAtUtc"));
        Assert.False(string.IsNullOrEmpty(json.GetProperty("rowVersion").GetString()));
    }

    [Fact]
    public async Task Review_UnknownListing_Returns404()
    {
        var admin = await _scenario.AdminAsync();

        var response = await admin.Client.GetAsync($"{Routes.AdminProperties}/{Guid.NewGuid()}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task Approve_CurrentRowVersion_PublishesListingAndNotifiesSeller()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id);

        var response = await admin.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await GetMineAsync(seller.Client, id);
        Assert.Equal(ModerationStatus.Approved, details.ModerationStatus);
        Assert.NotNull(details.ReviewedAtUtc);
        Assert.Equal(HttpStatusCode.OK, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
        Assert.Single(await NotificationsAboutAsync(seller.Client, id, NotificationType.PropertyApproved));
        Assert.NotEqual(rowVersion, await Scenario.ReviewVersionAsync(admin.Client, id));
    }

    [Fact]
    public async Task Approve_StaleRowVersionAfterImageChange_Returns409ChangedSinceReview()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var reviewed = await Scenario.ReviewVersionAsync(admin.Client, id);
        await Scenario.AddImageAsync(seller.Client, id);

        var stale = await admin.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion = reviewed });

        await AssertErrorAsync(stale, HttpStatusCode.Conflict, "Properties.ChangedSinceReview");
        Assert.Equal(ModerationStatus.Pending, (await GetMineAsync(seller.Client, id)).ModerationStatus);
        var fresh = await admin.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id) });
        Assert.Equal(HttpStatusCode.NoContent, fresh.StatusCode);
    }

    [Fact]
    public async Task Approve_StaleRowVersionAfterImageDeleted_Returns409ChangedSinceReview()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var extra = await Scenario.AddImageAsync(seller.Client, id);
        var reviewed = await Scenario.ReviewVersionAsync(admin.Client, id);
        Assert.Equal(HttpStatusCode.NoContent, (await seller.Client.DeleteAsync($"{Routes.Properties}/{id}/images/{extra}", Ct)).StatusCode);

        var stale = await admin.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion = reviewed });

        await AssertErrorAsync(stale, HttpStatusCode.Conflict, "Properties.ChangedSinceReview");
    }

    [Theory]
    [InlineData("not-base64!")]
    [InlineData("AAAA")]
    [InlineData("AAAAAAAAAAAAAAAA")]
    public async Task Approve_InvalidRowVersion_Returns400(string rowVersion)
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());

        var response = await admin.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion });

        await AssertValidationErrorAsync(response, "RowVersion");
        Assert.Equal(ModerationStatus.Pending, (await GetMineAsync(seller.Client, id)).ModerationStatus);
    }

    [Fact]
    public async Task Approve_UnknownListing_Returns404()
    {
        var admin = await _scenario.AdminAsync();

        var response = await admin.Client.PostJsonAsync(ApproveUrl(Guid.NewGuid()), new { rowVersion = UnknownRowVersion });

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task Approve_FewerThanThreeImages_Returns400MinImagesRequired()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        await Scenario.AddImageAsync(seller.Client, id);
        await Scenario.AddImageAsync(seller.Client, id);
        await Scenario.UploadDocumentAsync(seller.Client, id);

        var response = await admin.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id) });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "PropertyErrors.MinImagesRequired");
    }

    [Fact]
    public async Task Approve_WithoutOwnershipDocument_Returns400OwnershipDocumentRequired()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        for (var i = 0; i < 3; i++)
        {
            await Scenario.AddImageAsync(seller.Client, id);
        }

        var response = await admin.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id) });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "PropertyErrors.OwnershipDocumentRequired");
    }

    [Fact]
    public async Task Approve_AlreadyApproved_Returns409()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);

        var response = await admin.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id) });

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "PropertyErrors.AlreadyApproved");
    }

    [Fact]
    public async Task Approve_RejectedListing_Returns409CannotApproveRejectedProperty()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        await RejectAsync(admin.Client, id);

        var response = await admin.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id) });

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "PropertyErrors.CannotApproveRejectedProperty");
        Assert.Equal(ModerationStatus.Rejected, (await GetMineAsync(seller.Client, id)).ModerationStatus);
    }

    [Fact]
    public async Task Approve_AsUser_Returns403()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id);

        var response = await seller.Client.PostJsonAsync(ApproveUrl(id), new { rowVersion });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ModerationStatus.Pending, (await GetMineAsync(seller.Client, id)).ModerationStatus);
    }

    [Fact]
    public async Task Reject_WithReason_OwnerSeesTrimmedReasonAndIsNotified()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id);

        var response = await admin.Client.PostJsonAsync(RejectUrl(id), new { rejectionReason = "  وثيقة الملكية غير مقروءة  ", rowVersion });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await GetMineAsync(seller.Client, id);
        Assert.Equal(ModerationStatus.Rejected, details.ModerationStatus);
        Assert.Equal("وثيقة الملكية غير مقروءة", details.RejectionReason);
        Assert.NotNull(details.ReviewedAtUtc);
        var mine = Assert.Single(await GetMyListAsync(seller.Client));
        Assert.Equal("وثيقة الملكية غير مقروءة", mine.RejectionReason);
        Assert.Single(await NotificationsAboutAsync(seller.Client, id, NotificationType.PropertyRejected));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Reject_MissingReason_Returns400(string reason)
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id);

        var response = await admin.Client.PostJsonAsync(RejectUrl(id), new { rejectionReason = reason, rowVersion });

        await AssertValidationErrorAsync(response, "RejectionReason");
        Assert.Equal(ModerationStatus.Pending, (await GetMineAsync(seller.Client, id)).ModerationStatus);
    }

    [Fact]
    public async Task Reject_ReasonTooLong_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id);

        var response = await admin.Client.PostJsonAsync(RejectUrl(id), new { rejectionReason = new string('س', 501), rowVersion });

        await AssertValidationErrorAsync(response, "RejectionReason");
    }

    [Fact]
    public async Task Reject_InvalidRowVersion_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());

        var response = await admin.Client.PostJsonAsync(RejectUrl(id), new { rejectionReason = "سبب", rowVersion = "AAAA" });

        await AssertValidationErrorAsync(response, "RowVersion");
    }

    [Fact]
    public async Task Reject_StaleRowVersion_Returns409ChangedSinceReview()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var reviewed = await Scenario.ReviewVersionAsync(admin.Client, id);
        await Scenario.UploadDocumentAsync(seller.Client, id);

        var response = await admin.Client.PostJsonAsync(RejectUrl(id), new { rejectionReason = "سبب", rowVersion = reviewed });

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "Properties.ChangedSinceReview");
        Assert.Equal(ModerationStatus.Pending, (await GetMineAsync(seller.Client, id)).ModerationStatus);
    }

    [Fact]
    public async Task Reject_ApprovedListing_Returns409CannotRejectApprovedProperty()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);

        var response = await admin.Client.PostJsonAsync(RejectUrl(id), new { rejectionReason = "سبب", rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id) });

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "PropertyErrors.CannotRejectApprovedProperty");
    }

    [Fact]
    public async Task Reject_AlreadyRejected_Returns409()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        await RejectAsync(admin.Client, id);

        var response = await admin.Client.PostJsonAsync(RejectUrl(id), new { rejectionReason = "سبب آخر", rowVersion = await Scenario.ReviewVersionAsync(admin.Client, id) });

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "PropertyErrors.AlreadyRejected");
    }

    [Fact]
    public async Task Reject_AsUser_Returns403()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());

        var response = await seller.Client.PostJsonAsync(RejectUrl(id), new { rejectionReason = "سبب", rowVersion = UnknownRowVersion });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
