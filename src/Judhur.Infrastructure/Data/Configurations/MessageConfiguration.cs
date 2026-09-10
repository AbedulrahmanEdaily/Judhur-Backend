using Judhur.Domain.Conversations.Messages;
using Judhur.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(i => i.Id);
        // Derived from ReadAtUtc, never stored.
        builder.Ignore(m => m.IsRead);

        // The ConversationId foreign key comes from ConversationConfiguration.

        // Restrict, not Cascade: Users already reaches this table through
        // Conversations, and a second cascade path is rejected by SQL Server.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.Content).HasMaxLength(Message.MaxContentLength);

        // Reading a conversation: filter by id, ordered by send time.
        builder.HasIndex(m => new { m.ConversationId, m.SentAtUtc });
    }
}
