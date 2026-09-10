using Judhur.Domain.Reviews;
using Judhur.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        // A review of a deleted seller is meaningless, so it goes with them.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Two cascade paths from Users to the same row are rejected by SQL Server,
        // so this side is Restrict: deleting an author with reviews fails loudly
        // and the delete-account flow has to clear them first.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Rating).HasPrecision(2, 1);
        builder.Property(r => r.Comment).HasMaxLength(Review.MaxCommentLength);

        // SellerId leads: "reviews of this seller" is the dominant query.
        builder.HasIndex(r => new { r.SellerId, r.ReviewerId }).IsUnique();
    }
}
