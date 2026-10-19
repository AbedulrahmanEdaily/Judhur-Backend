using System.Globalization;

using Judhur.Domain.Common.Results;
using Judhur.Domain.Conversations;
using Judhur.Domain.Favorites;
using Judhur.Domain.Notifications;
using Judhur.Domain.Notifications.Enums;
using Judhur.Domain.Properties;
using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;
using Judhur.Domain.Reviews;

namespace Judhur.Infrastructure.Data.Seed;

internal sealed class DemoWorldBuilder
{
    public const string SeedPublicIdPrefix = "seed/";

    private static readonly TimeSpan ReviewDelay = TimeSpan.FromHours(3);
    private static readonly TimeSpan ReadDelay = TimeSpan.FromHours(2);
    private static readonly TimeSpan UnreadWindow = TimeSpan.FromHours(48);

    private readonly DateTimeOffset _now;
    private readonly Guid _adminId;
    private readonly IReadOnlyDictionary<string, Guid> _userIds;
    private readonly IReadOnlyDictionary<string, Guid> _propertyIds;
    private readonly IReadOnlyDictionary<string, string> _documentIds;
    private readonly Dictionary<string, Property> _properties = [];
    private readonly DemoWorld _world = new();

    private DemoWorldBuilder(
        DateTimeOffset now,
        Guid adminId,
        IReadOnlyDictionary<string, Guid> userIds,
        IReadOnlyDictionary<string, Guid> propertyIds,
        IReadOnlyDictionary<string, string> documentIds)
    {
        _now = now;
        _adminId = adminId;
        _userIds = userIds;
        _propertyIds = propertyIds;
        _documentIds = documentIds;
    }

    public static DemoWorld Build(
        DateTimeOffset now,
        Guid adminId,
        IReadOnlyDictionary<string, Guid> userIds,
        IReadOnlyDictionary<string, Guid> propertyIds,
        IReadOnlyDictionary<string, string> documentIds)
    {
        var builder = new DemoWorldBuilder(now, adminId, userIds, propertyIds, documentIds);
        builder.AddProperties();
        builder.AddConversations();
        builder.AddReviews();
        builder.AddReports();
        builder.AddBan();
        builder.AddFavorites();
        return builder._world;
    }

    private void AddProperties()
    {
        for (var index = 0; index < DemoListings.All.Length; index++)
        {
            var listing = DemoListings.All[index];
            var ordinal = DemoListings.All.Take(index).Count(l => l.Gallery == listing.Gallery);
            var property = BuildProperty(listing, ordinal);
            _properties.Add(listing.Key, property);
            _world.Properties.Add(property);
            _world.CreatedAt[property] = CreatedAt(listing);
        }
    }

    private Property BuildProperty(DemoListing listing, int galleryOrdinal)
    {
        var property = Ensure(Property.Create(
            _propertyIds[listing.Key],
            listing.Title,
            listing.Description,
            listing.Price,
            listing.PaymentType,
            listing.PropertyType,
            listing.PropertyStatus,
            listing.Area,
            listing.City,
            listing.Region,
            listing.FullAddress,
            listing.Latitude,
            listing.Longitude,
            listing.LandClassification,
            listing.LegalStatus,
            _userIds[listing.Seller]), listing.Key);

        var photos = DemoPhotos.For(listing.Gallery, galleryOrdinal, listing.ImageCount);
        for (var photo = 0; photo < photos.Count; photo++)
        {
            Ensure(property.AddImage(
                Guid.CreateVersion7(),
                photos[photo],
                $"{SeedPublicIdPrefix}{listing.Key}-{photo + 1}",
                isMainImage: false), listing.Key);
        }

        if (listing.HasOwnershipDocument)
        {
            Ensure(property.SetOwnershipDocument(_documentIds[listing.Key]), listing.Key);
        }

        var createdAt = CreatedAt(listing);
        var reviewedAt = createdAt + ReviewDelay;
        switch (listing.Outcome)
        {
            case DemoOutcome.Pending:
                if (listing.HasOwnershipDocument && photos.Count >= Property.MinImages)
                {
                    Notify(_adminId, NotificationType.PropertyPendingReview, "عقار بانتظار المراجعة",
                        $"العقار \"{listing.Title}\" جاهز للمراجعة.", property.Id, createdAt);
                }
                break;
            case DemoOutcome.Rejected:
                Ensure(property.Reject(_adminId, reviewedAt, listing.ModerationReason!), listing.Key);
                Notify(property.SellerId, NotificationType.PropertyRejected, "تم رفض عقارك",
                    $"تم رفض عقارك «{listing.Title}». السبب: {Sentence(listing.ModerationReason!)}. عدّل العقار ثم أعد إرساله للمراجعة.",
                    property.Id, reviewedAt);
                break;
            default:
                Ensure(property.Approve(_adminId, reviewedAt), listing.Key);
                Notify(property.SellerId, NotificationType.PropertyApproved, "تمت الموافقة على عقارك",
                    $"عقارك «{listing.Title}» أصبح منشوراً ويظهر الآن في نتائج البحث.", property.Id, reviewedAt);
                break;
        }

        switch (listing.Outcome)
        {
            case DemoOutcome.Deactivated:
                Ensure(property.Deactivate(), listing.Key);
                break;
            case DemoOutcome.Sold:
                Ensure(property.MarkAsSold(), listing.Key);
                break;
            case DemoOutcome.Rented:
                Ensure(property.MarkAsRented(), listing.Key);
                break;
            case DemoOutcome.Suspended:
                var suspendedAt = ReviewedAt(DemoActivity.Reports.Single(r => r.Listing == listing.Key && r.Outcome == ReportStatus.Resolved));
                Ensure(property.SuspendByAdmin(_adminId, suspendedAt, listing.ModerationReason!), listing.Key);
                Notify(property.SellerId, NotificationType.PropertySuspended, "تم إيقاف عقارك",
                    $"تم إيقاف عقارك «{listing.Title}» وإخفاؤه من نتائج البحث بعد مراجعة الإدارة. السبب: {Sentence(listing.ModerationReason!)}. عدّل العقار ثم أعد إرساله للمراجعة.",
                    property.Id, suspendedAt);
                break;
        }

        return property;
    }

    private void AddConversations()
    {
        foreach (var demo in DemoActivity.Conversations)
        {
            var property = _properties[demo.Listing];
            var buyerId = _userIds[demo.Buyer];
            var startedAt = _now.AddHours(-demo.StartedHoursAgo);
            if (startedAt <= _world.CreatedAt[property])
            {
                throw new InvalidOperationException($"Demo conversation on '{demo.Listing}' starts before the listing was published.");
            }

            var conversation = Ensure(Conversation.Create(Guid.CreateVersion7(), buyerId, property.SellerId, property.Id), demo.Listing);
            foreach (var message in demo.Messages)
            {
                var senderId = message.FromBuyer ? buyerId : property.SellerId;
                var sentAt = startedAt.AddMinutes(message.MinutesAfterStart);
                Ensure(conversation.MarkAsRead(senderId, sentAt), demo.Listing);
                Ensure(conversation.AddMessage(Guid.CreateVersion7(), senderId, message.Content, sentAt), demo.Listing);
            }

            var readAt = conversation.LastMessageAtUtc!.Value.AddMinutes(5);
            if (demo.SellerHasRead)
            {
                Ensure(conversation.MarkAsRead(property.SellerId, readAt), demo.Listing);
            }
            if (demo.BuyerHasRead)
            {
                Ensure(conversation.MarkAsRead(buyerId, readAt), demo.Listing);
            }

            _world.Conversations.Add(conversation);
            _world.CreatedAt[conversation] = startedAt;
        }
    }

    private void AddReviews()
    {
        foreach (var demo in DemoActivity.Reviews)
        {
            var reviewerId = _userIds[demo.Reviewer];
            var sellerId = _userIds[demo.Seller];
            var createdAt = _now.AddHours(-demo.HoursAgo);
            var eligible = _world.Conversations.Any(c =>
                c.BuyerId == reviewerId
                && c.SellerId == sellerId
                && c.Messages.Any(m => m.SenderId == sellerId && m.SentAtUtc < createdAt));
            if (!eligible)
            {
                throw new InvalidOperationException($"Demo review by '{demo.Reviewer}' has no earlier reply from '{demo.Seller}'.");
            }

            var review = Ensure(Review.Create(Guid.CreateVersion7(), reviewerId, sellerId, demo.Rating, demo.Comment), demo.Reviewer);
            _world.Reviews.Add(review);
            _world.CreatedAt[review] = createdAt;
            var rating = demo.Rating.ToString("0.#", CultureInfo.InvariantCulture);
            Notify(sellerId, NotificationType.NewReview, "حصلت على تقييم جديد",
                $"قيّمك أحد المشترين بـ {rating} من 5. يظهر التقييم في صفحتك العامة.", sellerId, createdAt);
        }
    }

    private void AddReports()
    {
        foreach (var demo in DemoActivity.Reports)
        {
            var property = _properties[demo.Listing];
            var listing = DemoListings.All.Single(l => l.Key == demo.Listing);
            var createdAt = _now.AddHours(-demo.HoursAgo);
            var report = Ensure(Report.Create(
                Guid.CreateVersion7(),
                _userIds[demo.Reporter],
                property.Id,
                property.SellerId,
                demo.Reason,
                demo.Details), demo.Listing);
            _world.Reports.Add(report);
            _world.CreatedAt[report] = createdAt;
            Notify(_adminId, NotificationType.NewReport, "بلاغ جديد",
                $"وصل بلاغ جديد على العقار \"{listing.Title}\" بسبب: {ReasonText(demo.Reason)}.", report.Id, createdAt);

            if (demo.Outcome == ReportStatus.Pending)
            {
                continue;
            }

            var reviewedAt = ReviewedAt(demo);
            Ensure(demo.Outcome == ReportStatus.Resolved
                ? report.Resolve(_adminId, reviewedAt, demo.AdminNote)
                : report.Dismiss(_adminId, reviewedAt, demo.AdminNote), demo.Listing);
            var outcome = demo.Outcome == ReportStatus.Resolved
                ? $"راجعنا بلاغك عن العقار «{listing.Title}» واتخذنا الإجراء المناسب. شكرًا لمساعدتك في الحفاظ على موثوقية جذور."
                : $"راجعنا بلاغك عن العقار «{listing.Title}» ولم نجد فيه ما يخالف شروط النشر.";
            var body = demo.AdminNote is { } note ? $"{outcome} ملاحظة الإدارة: {note}" : outcome;
            Notify(_userIds[demo.Reporter], NotificationType.ReportResolved, "تمت مراجعة بلاغك", body, report.Id, reviewedAt);
        }
    }

    private void AddBan()
    {
        var ban = DemoActivity.Ban;
        var userId = _userIds[ban.User];
        var bannedAt = ReviewedAt(DemoActivity.Reports.Single(r => r.Listing == ban.ReportedListing && r.Outcome == ReportStatus.Resolved));
        var deactivated = 0;
        foreach (var property in _world.Properties.Where(p => p.SellerId == userId && p.IsActive))
        {
            Ensure(property.Deactivate(), ban.User);
            deactivated++;
        }

        var body = $"تم حظر حسابك من قبل إدارة جذور. السبب: {Sentence(ban.Reason)}.";
        if (deactivated > 0)
        {
            body += $" تم إيقاف إعلاناتك ({deactivated}).";
        }
        Notify(userId, NotificationType.AccountBanned, "تم حظر حسابك", body, null, bannedAt);
        _world.Ban = new DemoBanState(userId, bannedAt, ban.Reason);
    }

    private void AddFavorites()
    {
        foreach (var demo in DemoActivity.Favorites)
        {
            var property = _properties[demo.Listing];
            var favorite = Ensure(Favorite.Create(Guid.CreateVersion7(), _userIds[demo.User], property.Id), demo.User);
            _world.Favorites.Add(favorite);
            var likedAt = _world.CreatedAt[property].AddHours(12);
            _world.CreatedAt[favorite] = likedAt < _now ? likedAt : _now.AddHours(-1);
        }
    }

    private void Notify(Guid userId, NotificationType type, string title, string body, Guid? referenceId, DateTimeOffset createdAt)
    {
        var notification = Ensure(Notification.Create(Guid.CreateVersion7(), userId, type, title, body, referenceId), title);
        if (createdAt < _now - UnreadWindow)
        {
            notification.MarkAsRead(createdAt + ReadDelay);
        }
        _world.Notifications.Add(notification);
        _world.CreatedAt[notification] = createdAt;
    }

    private DateTimeOffset CreatedAt(DemoListing listing) => _now.AddDays(-listing.DaysAgo);

    private DateTimeOffset ReviewedAt(DemoReport report) => _now.AddHours(report.ReviewedAfterHours - report.HoursAgo);

    private static string Sentence(string text) => text.TrimEnd('.', ' ');

    private static string ReasonText(ReportReason reason) => reason switch
    {
        ReportReason.Fraud => "احتيال",
        ReportReason.IncorrectInformation => "معلومات غير صحيحة",
        ReportReason.Duplicate => "إعلان مكرر",
        ReportReason.InappropriateContent => "محتوى غير لائق",
        _ => "سبب آخر"
    };

    private static T Ensure<T>(Result<T> result, string key) =>
        result.IsError
            ? throw new InvalidOperationException($"Demo data '{key}' violates a domain rule: {result.TopError.Code}")
            : result.Value;
}
