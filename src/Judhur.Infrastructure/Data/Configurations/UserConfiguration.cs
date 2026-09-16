using Judhur.Domain.Users;
using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(i => i.Id);
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<User>(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(u => u.Name).HasMaxLength(User.MaxNameLength);
        builder.Property(u => u.PhoneNumber).HasMaxLength(User.MaxPhoneNumberLength);
        builder.Property(u => u.City).HasMaxLength(User.MaxCityLength);
        builder.Property(u => u.BannedReason).HasMaxLength(User.MaxBanReasonLength);

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(u => u.Role);
        builder.HasIndex(u => u.IsBanned);
    }
}
