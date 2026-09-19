using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    private const int MaxFullNameLength = 150;
    private const int MaxCityLength = 100;
    private const int MaxProfileImageUrlLength = 500;
    private const int MaxResetCodeLength = 256;
    private const int MaxBioLength = 1000;
    private const int MaxPhoneNumberLength = 20;

    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AppUsers");

        builder.Property(u => u.FullName).HasMaxLength(MaxFullNameLength);
        builder.Property(u => u.City).HasMaxLength(MaxCityLength);
        builder.Property(u => u.ProfileImageUrl).HasMaxLength(MaxProfileImageUrlLength);
        builder.Property(u => u.ResetCode).HasMaxLength(MaxResetCodeLength);
        builder.Property(u => u.Bio).HasMaxLength(MaxBioLength);
        builder.Property(u => u.PhoneNumber).HasMaxLength(MaxPhoneNumberLength);
        
        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .HasFilter("[NormalizedEmail] IS NOT NULL")
            .IsUnique();
    }
}
