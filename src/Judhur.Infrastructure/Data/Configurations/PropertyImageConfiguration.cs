using Judhur.Domain.Properties.PropertyImages;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.Property(pr => pr.PublicId).HasMaxLength(200);
        builder.Property(pr => pr.FileUrl).HasMaxLength(500);
        builder.HasIndex(i => new { i.PropertyId, i.DisplayOrder }).IsUnique();
        builder.HasIndex(i => i.PropertyId)
            .IsUnique()
            .HasFilter("[IsMainImage] = 1");
    }
}