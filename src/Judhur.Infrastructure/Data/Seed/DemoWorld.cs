using Judhur.Domain.Common;
using Judhur.Domain.Conversations;
using Judhur.Domain.Favorites;
using Judhur.Domain.Notifications;
using Judhur.Domain.Properties;
using Judhur.Domain.Reports;
using Judhur.Domain.Reviews;

namespace Judhur.Infrastructure.Data.Seed;

internal sealed record DemoBanState(Guid UserId, DateTimeOffset BannedAtUtc, string Reason);

internal sealed class DemoWorld
{
    public List<Property> Properties { get; } = [];
    public List<Conversation> Conversations { get; } = [];
    public List<Review> Reviews { get; } = [];
    public List<Report> Reports { get; } = [];
    public List<Favorite> Favorites { get; } = [];
    public List<Notification> Notifications { get; } = [];
    public Dictionary<AuditableEntity, DateTimeOffset> CreatedAt { get; } = new(ReferenceEqualityComparer.Instance);
    public DemoBanState? Ban { get; set; }
}
