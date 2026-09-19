using Judhur.Domain.Notifications;
using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Ignore(n => n.IsRead);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(n => n.Title).HasMaxLength(Notification.MaxTitleLength);
        builder.Property(n => n.Body).HasMaxLength(Notification.MaxBodyLength);
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(n => new { n.UserId, n.ReadAtUtc });
        builder.HasIndex(n => new { n.UserId, n.CreatedAtUtc });
    }
}
