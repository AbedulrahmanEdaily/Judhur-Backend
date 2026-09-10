using Judhur.Domain.Identity;
using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(i => i.Id);
        // A refresh token belongs to the Identity account, not to the domain
        // profile, and dies with it.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.Token).HasMaxLength(RefreshToken.MaxTokenLength);

        // Every refresh request looks the token up by value, and two accounts
        // must never share one.
        builder.HasIndex(t => t.Token).IsUnique();

        // Cleaning out tokens that have already lapsed.
        builder.HasIndex(t => t.ExpiresOnUtc);
    }
}
