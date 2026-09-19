using Judhur.Domain.Reviews;
using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Rating).HasPrecision(2, 1);
        builder.Property(r => r.Comment).HasMaxLength(Review.MaxCommentLength);
        builder.HasIndex(r => new { r.SellerId, r.ReviewerId }).IsUnique();
    }
}
