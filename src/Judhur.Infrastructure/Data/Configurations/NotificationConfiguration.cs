using Judhur.Domain.Notifications;
using Judhur.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        // Derived from ReadAtUtc, never stored.
        builder.Ignore(n => n.IsRead);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReferenceId is intentionally left without a foreign key: the table it
        // points at depends on Type, and a dead link is handled as a 404 by the
        // client rather than by referential integrity.

        builder.Property(n => n.Title).HasMaxLength(Notification.MaxTitleLength);
        builder.Property(n => n.Body).HasMaxLength(Notification.MaxBodyLength);

        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(32);

        // Unread badge count and the notification list, newest first.
        builder.HasIndex(n => new { n.UserId, n.ReadAtUtc });
        builder.HasIndex(n => new { n.UserId, n.CreatedAtUtc });
    }
}
