using Judhur.Domain.Reports.Enums;

namespace Judhur.Infrastructure.Data.Seed;

internal sealed record DemoMessage(bool FromBuyer, string Content, int MinutesAfterStart);

internal sealed record DemoConversation(
    string Buyer,
    string Listing,
    int StartedHoursAgo,
    bool SellerHasRead,
    bool BuyerHasRead,
    DemoMessage[] Messages);

internal sealed record DemoReview(string Reviewer, string Seller, decimal Rating, string? Comment, int HoursAgo);

internal sealed record DemoReport(
    string Reporter,
    string Listing,
    ReportReason Reason,
    string? Details,
    int HoursAgo,
    ReportStatus Outcome,
    string? AdminNote = null,
    int ReviewedAfterHours = 20);

internal sealed record DemoFavorite(string User, string Listing);

internal sealed record DemoBan(string User, string Reason, string ReportedListing);
