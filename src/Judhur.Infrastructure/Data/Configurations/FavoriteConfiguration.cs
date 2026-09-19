using Judhur.Domain.Favorites;
using Judhur.Domain.Properties;
using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Property>().WithMany().HasForeignKey(f => f.PropertyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(f => new { f.UserId, f.PropertyId }).IsUnique();
    }
}