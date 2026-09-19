using Judhur.Domain.Conversations.Messages;
using Judhur.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Judhur.Infrastructure.Data.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Ignore(m => m.IsRead);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(m => m.Content).HasMaxLength(Message.MaxContentLength);
        builder.HasIndex(m => new { m.ConversationId, m.SentAtUtc });
    }
}
