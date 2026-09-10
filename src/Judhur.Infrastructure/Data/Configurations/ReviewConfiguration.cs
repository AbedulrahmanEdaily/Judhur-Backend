using Judhur.Domain.Properties;
using Judhur.Domain.Reviews;
using Judhur.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(r => r.Rating).HasPrecision(2, 1);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict); builder.HasOne<User>().WithMany().HasForeignKey(r => r.SellerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => new { r.SellerId, r.ReviewerId }).IsUnique();
    }
}