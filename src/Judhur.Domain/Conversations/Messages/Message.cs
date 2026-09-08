using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Conversations.Messages;

public sealed class Message : Entity
{
    public const int MaxContentLength = 2000;
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTimeOffset SentAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public bool IsRead => ReadAtUtc is not null;
    private Message() { }
    private Message(Guid id, Guid conversationId, Guid senderId, string content, DateTimeOffset sentAtUtc) : base(id)
    {
        ConversationId = conversationId;
        SenderId = senderId;
        Content = content;
        SentAtUtc = sentAtUtc;
    }
    internal static Result<Message> Create(Guid id, Guid conversationId, Guid senderId, string content, DateTimeOffset sentAtUtc)
    {
        if (conversationId == Guid.Empty)
        {
            return MessageErrors.ConversationIdRequired;
        }
        if (senderId == Guid.Empty)
        {
            return MessageErrors.SenderIdRequired;
        }
        if (string.IsNullOrWhiteSpace(content))
        {
            return MessageErrors.ContentRequired;
        }
        if (content.Length > MaxContentLength)
        {
            return MessageErrors.ContentTooLong;
        }
        return new Message(id, conversationId, senderId, content, sentAtUtc);
    }
    internal void MarkAsRead(DateTimeOffset readAtUtc)
    {
        if (IsRead)
        {
            return;
        }
        ReadAtUtc = readAtUtc;
    }
}