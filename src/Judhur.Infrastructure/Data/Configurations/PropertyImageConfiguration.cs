using Judhur.Domain.Properties.PropertyImages;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FileUrl).HasMaxLength(PropertyImage.MaxFileUrlLength);
        builder.Property(i => i.PublicId).HasMaxLength(PropertyImage.MaxPublicIdLength);

        // The PropertyId foreign key and its index come from PropertyConfiguration.

        // Property.AddImage hands out Max(DisplayOrder) + 1; this is the guarantee.
        builder.HasIndex(i => new { i.PropertyId, i.DisplayOrder }).IsUnique();

        // Filtered unique index: at most one row per property may have
        // IsMainImage = 1, so "exactly one main image" is enforced by the engine
        // and not only by Property.AddImage / SetMainImage.
        builder.HasIndex(i => i.PropertyId)
            .IsUnique()
            .HasFilter("[IsMainImage] = 1");
    }
}
