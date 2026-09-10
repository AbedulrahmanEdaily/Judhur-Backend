using Judhur.Domain.Properties;
using Judhur.Domain.Reports;
using Judhur.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        // A report is a pure relationship: it has no meaning once either the
        // reporter or the reported listing is gone.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReviewedBy is deliberately not a foreign key: it is an audit trail,
        // and it must survive the admin account that produced it.

        builder.Property(r => r.Details).HasMaxLength(Report.MaxDetailsLength);
        builder.Property(r => r.AdminNote).HasMaxLength(Report.MaxAdminNoteLength);

        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);

        // Enforces ReportErrors.DuplicateReport.
        builder.HasIndex(r => new { r.ReporterId, r.PropertyId }).IsUnique();

        // The admin moderation queue: pending reports, oldest first.
        builder.HasIndex(r => new { r.Status, r.CreatedAtUtc });
    }
}
