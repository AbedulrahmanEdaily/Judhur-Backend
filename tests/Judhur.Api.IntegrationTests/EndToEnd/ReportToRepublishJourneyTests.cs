using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

using static Judhur.Api.IntegrationTests.Social.SocialApi;

namespace Judhur.Api.IntegrationTests.EndToEnd;

[Collection(ApiCollection.Name)]
public sealed class ReportToRepublishJourneyTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Journey_ReportedListingIsSuspendedFixedAndPublishedAgain()
    {
        var anonymous = _scenario.Anonymous();
        var admin = await _scenario.AdminAsync();
        var seller = await _scenario.NewUserAsync("journey-owner");
        var reporter = await _scenario.NewUserAsync("journey-reporter");
        var listing = await PublishedListingAsync(_scenario, seller);
        Assert.True(await IsInSearchAsync(anonymous, listing.Token));

        var reportId = await CreateReportAsync(reporter.Client, listing.Id);
        var newReport = await WaitForNotificationAsync(admin.Client, "NewReport", reportId);
        Assert.Contains(listing.Title, Text(newReport, "body"));
        Assert.Equal("Pending", Text(await MyReportAsync(reporter.Client, reportId), "status"));

        const string suspension = "الصور لا تعود لهذا العقار";
        var resolve = await admin.Client.PostJsonAsync(
            $"{Routes.AdminReports}/{reportId}/resolve",
            new { adminNote = "تم إيقاف الإعلان مؤقتًا", suspendProperty = true, suspensionReason = suspension });
        Assert.Equal(HttpStatusCode.NoContent, resolve.StatusCode);

        await WaitForNotificationAsync(reporter.Client, "ReportResolved", reportId);
        Assert.Equal("Resolved", Text(await MyReportAsync(reporter.Client, reportId), "status"));
        var suspended = await WaitForNotificationAsync(seller.Client, "PropertySuspended", listing.Id);
        Assert.Contains(suspension, Text(suspended, "body"));
        var hidden = await MyListingAsync(seller.Client, listing.Id);
        Assert.Equal("Rejected", Text(hidden, "moderationStatus"));
        Assert.Equal(suspension, Text(hidden, "rejectionReason"));
        Assert.Equal(HttpStatusCode.NotFound, await PublicDetailsStatusAsync(anonymous, listing.Id));
        Assert.False(await IsInSearchAsync(anonymous, listing.Token));

        await Scenario.AddImageAsync(seller.Client, listing.Id);
        var resubmitted = await MyListingAsync(seller.Client, listing.Id);
        Assert.Equal("Pending", Text(resubmitted, "moderationStatus"));
        Assert.True(IsMissing(resubmitted, "rejectionReason"));
        var queued = await WaitForNotificationsAsync(admin.Client, "PropertyPendingReview", listing.Id, atLeast: 2);
        Assert.True(queued.Count >= 2);
        Assert.NotNull(await FindAsync(admin.Client, $"{Routes.AdminProperties}/pending", listing.Id));

        var staleVersion = await Scenario.ReviewVersionAsync(admin.Client, listing.Id);
        await Scenario.AddImageAsync(seller.Client, listing.Id);
        var stale = await admin.Client.PostJsonAsync($"{Routes.AdminProperties}/{listing.Id}/approve", new { rowVersion = staleVersion });
        await AssertProblemAsync(stale, HttpStatusCode.Conflict, "Properties.ChangedSinceReview");

        await Scenario.ApproveAsync(admin.Client, listing.Id);
        var approvals = await WaitForNotificationsAsync(seller.Client, "PropertyApproved", listing.Id, atLeast: 2);
        Assert.True(approvals.Count >= 2);
        Assert.Equal(HttpStatusCode.OK, await PublicDetailsStatusAsync(anonymous, listing.Id));
        Assert.True(await IsInSearchAsync(anonymous, listing.Token));
        Assert.Equal(5, (await GetJsonAsync(anonymous, $"{Routes.Properties}/{listing.Id}")).GetProperty("images").GetArrayLength());
    }
}
