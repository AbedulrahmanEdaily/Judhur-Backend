using Judhur.Domain.Conversations;
using Judhur.Domain.Properties;
using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.BuyerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => new { c.BuyerId, c.SellerId, c.PropertyId }).IsUnique();
        builder.HasIndex(c => c.LastMessageAtUtc);
    }
}
