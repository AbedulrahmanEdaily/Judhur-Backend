using Judhur.Domain.Conversations;
using Judhur.Domain.Favorites;
using Judhur.Domain.Identity;
using Judhur.Domain.Notifications;
using Judhur.Domain.Properties;
using Judhur.Domain.Reports;
using Judhur.Domain.Reviews;

using Microsoft.EntityFrameworkCore;

namespace Judhur.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Property> Properties { get; }

    DbSet<Conversation> Conversations { get; }

    DbSet<Review> Reviews { get; }

    DbSet<Report> Reports { get; }

    DbSet<Favorite> Favorites { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
