using Judhur.Domain.Properties.PropertyImages;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.FileUrl).HasMaxLength(PropertyImage.MaxFileUrlLength);
        builder.Property(i => i.PublicId).HasMaxLength(PropertyImage.MaxPublicIdLength);
        builder.HasIndex(i => new { i.PropertyId, i.DisplayOrder }).IsUnique();
        builder.HasIndex(i => i.PropertyId)
            .IsUnique()
            .HasFilter("[IsMainImage] = 1");
    }
}
