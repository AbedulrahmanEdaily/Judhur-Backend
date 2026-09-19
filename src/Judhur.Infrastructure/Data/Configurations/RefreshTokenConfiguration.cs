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
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(t => t.Token).HasMaxLength(RefreshToken.MaxTokenLength);
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.ExpiresOnUtc);
    }
}
