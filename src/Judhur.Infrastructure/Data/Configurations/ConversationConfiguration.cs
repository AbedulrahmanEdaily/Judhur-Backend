using Judhur.Domain.Conversations;
using Judhur.Domain.Properties;
using Judhur.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        // Child entity: no inverse navigation, written through the private field.
        builder.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.BuyerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Second path from Users to the same row, so this side cannot cascade
        // as well; deleting a seller with conversations fails until they are
        // cleared explicitly.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enforces ConversationErrors.ConversationAlreadyExists.
        builder.HasIndex(c => new { c.BuyerId, c.SellerId, c.PropertyId }).IsUnique();

        // "My conversations", newest activity first.
        builder.HasIndex(c => c.LastMessageAtUtc);
    }
}
