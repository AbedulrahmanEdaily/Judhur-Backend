using Judhur.Domain.Properties;
using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Ignore(p => p.MainImage);
        builder.HasMany(p => p.PropertyImages)
            .WithOne()
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.PropertyImages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(p => p.Title).HasMaxLength(Property.MaxTitleLength);
        builder.Property(p => p.Description).HasMaxLength(Property.MaxDescriptionLength);
        builder.Property(p => p.City).HasMaxLength(Property.MaxCityLength);
        builder.Property(p => p.Region).HasMaxLength(Property.MaxRegionLength);
        builder.Property(p => p.FullAddress).HasMaxLength(Property.MaxFullAddressLength);
        builder.Property(p => p.OwnershipDocumentUrl).HasMaxLength(Property.MaxOwnershipDocumentUrlLength);
        builder.Property(p => p.RejectionReason).HasMaxLength(Property.MaxRejectionReasonLength);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Area).HasPrecision(10, 2);
        builder.Property(p => p.PaymentType).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.PropertyType).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.PropertyStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.LandClassification).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.LegalStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.ModerationStatus).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(p => new { p.ModerationStatus, p.IsActive });
        builder.HasIndex(p => p.City);
        builder.HasIndex(p => new { p.Latitude, p.Longitude });
    }
}
