using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

using static Judhur.Api.IntegrationTests.Social.SocialApi;

namespace Judhur.Api.IntegrationTests.Social;

[Collection(ApiCollection.Name)]
public sealed class ReportsTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Create_PublishedListing_Returns201AndListsItInMyReports()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var listing = await PublishedListingAsync(_scenario, seller);

        var response = await ReportAsync(reporter.Client, listing.Id, "IncorrectInformation", "  المساحة الحقيقية أصغر  ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reportId = (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, reportId);
        var mine = await MyReportAsync(reporter.Client, reportId);
        Assert.Equal(listing.Id, mine.GetProperty("propertyId").GetGuid());
        Assert.Equal(listing.Title, Text(mine, "propertyTitle"));
        Assert.Equal("IncorrectInformation", Text(mine, "reason"));
        Assert.Equal("المساحة الحقيقية أصغر", Text(mine, "details"));
        Assert.Equal("Pending", Text(mine, "status"));
        Assert.True(IsMissing(mine, "adminNote"));
        Assert.True(IsMissing(mine, "reviewedAtUtc"));
    }

    [Fact]
    public async Task Create_ListingNotApproved_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var listing = await ReadyListingAsync(seller);

        var response = await ReportAsync(reporter.Client, listing.Id);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task Create_UnknownListing_Returns404()
    {
        var reporter = await _scenario.NewUserAsync("reporter");

        var response = await ReportAsync(reporter.Client, Guid.NewGuid());

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task Create_OwnListing_Returns409CannotReportOwnProperty()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var listing = await PublishedListingAsync(_scenario, seller);

        var response = await ReportAsync(seller.Client, listing.Id);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ReportErrors.CannotReportOwnProperty");
    }

    [Fact]
    public async Task Create_WhileEarlierReportPending_Returns409DuplicateReport()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var listing = await PublishedListingAsync(_scenario, seller);
        await CreateReportAsync(reporter.Client, listing.Id);

        var response = await ReportAsync(reporter.Client, listing.Id, "Duplicate", null);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ReportErrors.DuplicateReport");
    }

    [Fact]
    public async Task Create_AfterEarlierReportDismissed_Returns201()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var first = await CreateReportAsync(reporter.Client, listing.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(admin.Client, $"{Routes.AdminReports}/{first}/dismiss")).StatusCode);

        var response = await ReportAsync(reporter.Client, listing.Id, "Duplicate", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var second = (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task Create_OtherReasonWithoutDetails_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var listing = await PublishedListingAsync(_scenario, seller);

        var response = await ReportAsync(reporter.Client, listing.Id, "Other", null);

        await AssertValidationErrorAsync(response, "Details");
    }

    [Fact]
    public async Task Create_DetailsTooLong_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var listing = await PublishedListingAsync(_scenario, seller);

        var response = await ReportAsync(reporter.Client, listing.Id, "Fraud", new string('ن', 1001));

        await AssertValidationErrorAsync(response, "Details");
    }

    [Fact]
    public async Task Create_UnknownReason_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var listing = await PublishedListingAsync(_scenario, seller);

        var response = await reporter.Client.PostJsonAsync(Routes.Reports, new { propertyId = listing.Id, reason = 99, details = "x" });

        await AssertValidationErrorAsync(response, "Reason");
    }

    [Fact]
    public async Task Create_EmptyPropertyId_Returns400()
    {
        var reporter = await _scenario.NewUserAsync("reporter");

        var response = await ReportAsync(reporter.Client, Guid.Empty);

        await AssertValidationErrorAsync(response, "PropertyId");
    }

    [Fact]
    public async Task Create_Anonymous_Returns401()
    {
        var response = await ReportAsync(_scenario.Anonymous(), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_NewReport_NotifiesAdmin()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);

        var reportId = await CreateReportAsync(reporter.Client, listing.Id, "Fraud");

        var notification = await WaitForNotificationAsync(admin.Client, "NewReport", reportId);
        Assert.Equal("بلاغ جديد", Text(notification, "title"));
        Assert.Contains(listing.Title, Text(notification, "body"));
        Assert.Contains("احتيال", Text(notification, "body"));
        Assert.False(notification.GetProperty("isRead").GetBoolean());
    }

    [Fact]
    public async Task AdminList_Pending_ContainsReportWithPendingCountOfProperty()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var first = await _scenario.NewUserAsync("reporter");
        var second = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var firstReport = await CreateReportAsync(first.Client, listing.Id, "Fraud");
        var secondReport = await CreateReportAsync(second.Client, listing.Id, "Duplicate", null);

        var item = await SingleAsync(admin.Client, $"{Routes.AdminReports}?status=Pending", firstReport);

        Assert.Equal(listing.Id, item.GetProperty("propertyId").GetGuid());
        Assert.Equal(listing.Title, Text(item, "propertyTitle"));
        Assert.False(item.GetProperty("propertyIsDeleted").GetBoolean());
        Assert.Equal("Fraud", Text(item, "reason"));
        Assert.Equal("Pending", Text(item, "status"));
        Assert.Equal(2, item.GetProperty("pendingReportsOnProperty").GetInt32());
        Assert.NotNull(await FindAsync(admin.Client, Routes.AdminReports, secondReport));
        Assert.Null(await FindAsync(admin.Client, $"{Routes.AdminReports}?status=Resolved", firstReport));
    }

    [Fact]
    public async Task AdminList_InvalidPageSize_Returns400()
    {
        var admin = await _scenario.AdminAsync();

        var response = await admin.Client.GetAsync($"{Routes.AdminReports}?pageSize=0", Ct);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pagination.PageSizeInvalid");
    }

    [Fact]
    public async Task Details_PendingReport_ReturnsReporterPropertyAndOtherPendingCount()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter", "مبلّغ الاختبار");
        var other = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var reportId = await CreateReportAsync(reporter.Client, listing.Id, "InappropriateContent", "صور غير لائقة");
        await CreateReportAsync(other.Client, listing.Id);

        var details = await ReportDetailsAsync(admin.Client, reportId);

        Assert.Equal(reportId, details.GetProperty("id").GetGuid());
        Assert.Equal("InappropriateContent", Text(details, "reason"));
        Assert.Equal("صور غير لائقة", Text(details, "details"));
        Assert.Equal("Pending", Text(details, "status"));
        Assert.Equal(reporter.Id, details.GetProperty("reporterId").GetGuid());
        Assert.Equal("مبلّغ الاختبار", Text(details.GetProperty("reporter"), "fullName"));
        var property = details.GetProperty("property");
        Assert.Equal(listing.Id, property.GetProperty("id").GetGuid());
        Assert.Equal(listing.Title, Text(property, "title"));
        Assert.Equal("Approved", Text(property, "moderationStatus"));
        Assert.True(property.GetProperty("isActive").GetBoolean());
        Assert.False(property.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(seller.Id, property.GetProperty("sellerId").GetGuid());
        Assert.Equal(1, details.GetProperty("otherPendingReportsOnProperty").GetInt32());
    }

    [Fact]
    public async Task Details_UnknownReport_Returns404()
    {
        var admin = await _scenario.AdminAsync();

        var response = await admin.Client.GetAsync($"{Routes.AdminReports}/{Guid.NewGuid()}", Ct);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "ReportErrors.NotFound");
    }

    [Fact]
    public async Task Dismiss_WithoutBody_ClosesOnlyThisReportAndNotifiesReporter()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var other = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var reportId = await CreateReportAsync(reporter.Client, listing.Id);
        var otherId = await CreateReportAsync(other.Client, listing.Id);

        var response = await PostEmptyAsync(admin.Client, $"{Routes.AdminReports}/{reportId}/dismiss");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await ReportDetailsAsync(admin.Client, reportId);
        Assert.Equal("Dismissed", Text(details, "status"));
        Assert.True(IsMissing(details, "adminNote"));
        Assert.False(IsMissing(details, "reviewedAtUtc"));
        Assert.Equal("Pending", Text(await ReportDetailsAsync(admin.Client, otherId), "status"));
        Assert.NotNull(await FindAsync(admin.Client, $"{Routes.AdminReports}?status=Dismissed", reportId));
        var mine = await MyReportAsync(reporter.Client, reportId);
        Assert.Equal("Dismissed", Text(mine, "status"));
        Assert.False(IsMissing(mine, "reviewedAtUtc"));
        var notification = await WaitForNotificationAsync(reporter.Client, "ReportResolved", reportId);
        Assert.Equal("تمت مراجعة بلاغك", Text(notification, "title"));
        Assert.Contains(listing.Title, Text(notification, "body"));
        Assert.Contains("لم نجد فيه ما يخالف", Text(notification, "body"));
        Assert.Empty(await NotificationsAsync(other.Client, "ReportResolved"));
        Assert.Equal(HttpStatusCode.OK, await PublicDetailsStatusAsync(_scenario.Anonymous(), listing.Id));
    }

    [Fact]
    public async Task Dismiss_WithNote_ReporterSeesNote()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var reportId = await CreateReportAsync(reporter.Client, listing.Id);

        var response = await admin.Client.PostJsonAsync($"{Routes.AdminReports}/{reportId}/dismiss", new { adminNote = "  المعلومات مطابقة للوثائق  " });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var mine = await MyReportAsync(reporter.Client, reportId);
        Assert.Equal("Dismissed", Text(mine, "status"));
        Assert.Equal("المعلومات مطابقة للوثائق", Text(mine, "adminNote"));
        var notification = await WaitForNotificationAsync(reporter.Client, "ReportResolved", reportId);
        Assert.Contains("المعلومات مطابقة للوثائق", Text(notification, "body"));
    }

    [Fact]
    public async Task Dismiss_NoteTooLong_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var reportId = await CreateReportAsync(reporter.Client, listing.Id);

        var response = await admin.Client.PostJsonAsync($"{Routes.AdminReports}/{reportId}/dismiss", new { adminNote = new string('م', 1001) });

        await AssertValidationErrorAsync(response, "AdminNote");
        Assert.Equal("Pending", Text(await ReportDetailsAsync(admin.Client, reportId), "status"));
    }

    [Fact]
    public async Task Resolve_WithSuspension_RejectsListingHidesItAndNotifiesReportersAndOwner()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var other = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var reportId = await CreateReportAsync(reporter.Client, listing.Id);
        var otherId = await CreateReportAsync(other.Client, listing.Id, "IncorrectInformation", null);
        Assert.True(await IsInSearchAsync(_scenario.Anonymous(), listing.Token));
        const string reason = "الصور لا تعود لهذا العقار";

        var response = await admin.Client.PostJsonAsync(
            $"{Routes.AdminReports}/{reportId}/resolve",
            new { adminNote = "تم إيقاف الإعلان", suspendProperty = true, suspensionReason = reason });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Resolved", Text(await ReportDetailsAsync(admin.Client, reportId), "status"));
        var related = await ReportDetailsAsync(admin.Client, otherId);
        Assert.Equal("Resolved", Text(related, "status"));
        Assert.Equal("تم إيقاف الإعلان", Text(related, "adminNote"));
        Assert.NotNull(await FindAsync(admin.Client, $"{Routes.AdminReports}?status=Resolved", reportId));
        Assert.Null(await FindAsync(admin.Client, Routes.AdminReports, otherId));
        var owned = await MyListingAsync(seller.Client, listing.Id);
        Assert.Equal("Rejected", Text(owned, "moderationStatus"));
        Assert.Equal(reason, Text(owned, "rejectionReason"));
        Assert.Equal(HttpStatusCode.NotFound, await PublicDetailsStatusAsync(_scenario.Anonymous(), listing.Id));
        Assert.Equal(HttpStatusCode.NotFound, await PublicDetailsStatusAsync(reporter.Client, listing.Id));
        Assert.False(await IsInSearchAsync(_scenario.Anonymous(), listing.Token));
        var reporterNotification = await WaitForNotificationAsync(reporter.Client, "ReportResolved", reportId);
        Assert.Contains("اتخذنا الإجراء المناسب", Text(reporterNotification, "body"));
        Assert.Contains("تم إيقاف الإعلان", Text(reporterNotification, "body"));
        await WaitForNotificationAsync(other.Client, "ReportResolved", otherId);
        var suspended = await WaitForNotificationAsync(seller.Client, "PropertySuspended", listing.Id);
        Assert.Equal("تم إيقاف عقارك", Text(suspended, "title"));
        Assert.Contains(listing.Title, Text(suspended, "body"));
        Assert.Contains(reason, Text(suspended, "body"));
    }

    [Fact]
    public async Task Resolve_WithoutBody_ResolvesAndKeepsListingPublished()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var reportId = await CreateReportAsync(reporter.Client, listing.Id);

        var response = await PostEmptyAsync(admin.Client, $"{Routes.AdminReports}/{reportId}/resolve");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var mine = await MyReportAsync(reporter.Client, reportId);
        Assert.Equal("Resolved", Text(mine, "status"));
        Assert.True(IsMissing(mine, "adminNote"));
        Assert.Equal("Approved", Text(await MyListingAsync(seller.Client, listing.Id), "moderationStatus"));
        Assert.Equal(HttpStatusCode.OK, await PublicDetailsStatusAsync(_scenario.Anonymous(), listing.Id));
        await WaitForNotificationAsync(reporter.Client, "ReportResolved", reportId);
        Assert.Empty(await NotificationsAsync(seller.Client, "PropertySuspended", listing.Id));
    }

    [Fact]
    public async Task Resolve_SuspendWithoutReason_Returns400AndChangesNothing()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var reportId = await CreateReportAsync(reporter.Client, listing.Id);

        var response = await admin.Client.PostJsonAsync($"{Routes.AdminReports}/{reportId}/resolve", new { suspendProperty = true });

        await AssertValidationErrorAsync(response, "SuspensionReason");
        Assert.Equal("Pending", Text(await ReportDetailsAsync(admin.Client, reportId), "status"));
        Assert.Equal("Approved", Text(await MyListingAsync(seller.Client, listing.Id), "moderationStatus"));
    }

    [Fact]
    public async Task Resolve_SuspendListingAlreadyHidden_ResolvesWithoutSuspending()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var reportId = await CreateReportAsync(reporter.Client, listing.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await seller.Client.DeleteAsync($"{Routes.Properties}/{listing.Id}", Ct)).StatusCode);

        var response = await admin.Client.PostJsonAsync(
            $"{Routes.AdminReports}/{reportId}/resolve",
            new { suspendProperty = true, suspensionReason = "مخالفة" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await ReportDetailsAsync(admin.Client, reportId);
        Assert.Equal("Resolved", Text(details, "status"));
        Assert.True(details.GetProperty("property").GetProperty("isDeleted").GetBoolean());
        Assert.Equal("Approved", Text(details.GetProperty("property"), "moderationStatus"));
        Assert.True((await SingleAsync(admin.Client, $"{Routes.AdminReports}?status=Resolved", reportId)).GetProperty("propertyIsDeleted").GetBoolean());
        Assert.Equal(listing.Title, Text(await MyReportAsync(reporter.Client, reportId), "propertyTitle"));
        Assert.Empty(await NotificationsAsync(seller.Client, "PropertySuspended", listing.Id));
    }

    [Fact]
    public async Task Review_AlreadyReviewedReport_Returns409AlreadyReviewed()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var reporter = await _scenario.NewUserAsync("reporter");
        var admin = await _scenario.AdminAsync();
        var listing = await PublishedListingAsync(_scenario, seller);
        var reportId = await CreateReportAsync(reporter.Client, listing.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(admin.Client, $"{Routes.AdminReports}/{reportId}/dismiss")).StatusCode);

        var resolve = await PostEmptyAsync(admin.Client, $"{Routes.AdminReports}/{reportId}/resolve");
        var dismiss = await PostEmptyAsync(admin.Client, $"{Routes.AdminReports}/{reportId}/dismiss");

        await AssertProblemAsync(resolve, HttpStatusCode.Conflict, "ReportErrors.AlreadyReviewed");
        await AssertProblemAsync(dismiss, HttpStatusCode.Conflict, "ReportErrors.AlreadyReviewed");
        Assert.Equal("Dismissed", Text(await ReportDetailsAsync(admin.Client, reportId), "status"));
    }

    [Fact]
    public async Task Review_UnknownReport_Returns404()
    {
        var admin = await _scenario.AdminAsync();

        var dismiss = await PostEmptyAsync(admin.Client, $"{Routes.AdminReports}/{Guid.NewGuid()}/dismiss");
        var resolve = await PostEmptyAsync(admin.Client, $"{Routes.AdminReports}/{Guid.NewGuid()}/resolve");

        await AssertProblemAsync(dismiss, HttpStatusCode.NotFound, "ReportErrors.NotFound");
        await AssertProblemAsync(resolve, HttpStatusCode.NotFound, "ReportErrors.NotFound");
    }

    [Fact]
    public async Task AdminEndpoints_UserRole_Returns403()
    {
        var user = await _scenario.NewUserAsync("user");

        var list = await user.Client.GetAsync(Routes.AdminReports, Ct);
        var dismiss = await PostEmptyAsync(user.Client, $"{Routes.AdminReports}/{Guid.NewGuid()}/dismiss");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, dismiss.StatusCode);
    }

    [Fact]
    public async Task UserEndpoints_AdminRole_Returns403()
    {
        var admin = await _scenario.AdminAsync();

        var response = await ReportAsync(admin.Client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
