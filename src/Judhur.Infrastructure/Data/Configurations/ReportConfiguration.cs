using Judhur.Domain.Properties;
using Judhur.Domain.Reports;
using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(r => r.Details).HasMaxLength(Report.MaxDetailsLength);
        builder.Property(r => r.AdminNote).HasMaxLength(Report.MaxAdminNoteLength);
        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(r => new { r.ReporterId, r.PropertyId }).IsUnique();
        builder.HasIndex(r => new { r.Status, r.CreatedAtUtc });
    }
}
