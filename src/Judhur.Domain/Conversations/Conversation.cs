using Judhur.Domain.Common;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Conversations.Messages;

namespace Judhur.Domain.Conversations;

public sealed class Conversation : AuditableEntity
{
    public Guid BuyerId { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid PropertyId { get; private set; }
    public DateTimeOffset? LastMessageAtUtc { get; private set; }
    private readonly List<Message> _messages = [];
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();
    private Conversation() { }
    private Conversation(Guid id, Guid buyerId, Guid sellerId, Guid propertyId) : base(id)
    {
        BuyerId = buyerId;
        SellerId = sellerId;
        PropertyId = propertyId;
    }
    public static Result<Conversation> Create(Guid id, Guid buyerId, Guid sellerId, Guid propertyId)
    {
        if (buyerId == Guid.Empty)
        {
            return ConversationErrors.BuyerIdRequired;
        }
        if (sellerId == Guid.Empty)
        {
            return ConversationErrors.SellerIdRequired;
        }
        if (propertyId == Guid.Empty)
        {
            return ConversationErrors.PropertyIdRequired;
        }
        if (buyerId == sellerId)
        {
            return ConversationErrors.CannotStartConversationWithSelf;
        }
        return new Conversation(id, buyerId, sellerId, propertyId);
    }
    public Result<Message> AddMessage(Guid messageId, Guid senderId, string content, DateTimeOffset sentAtUtc)
    {
        if (!IsParticipant(senderId))
        {
            return ConversationErrors.NotParticipant;
        }
        var messageResult = Message.Create(messageId, Id, senderId, content, sentAtUtc);
        if (messageResult.IsError)
        {
            return messageResult.Errors;
        }
        var message = messageResult.Value;
        _messages.Add(message);
        LastMessageAtUtc = sentAtUtc;
        return message;
    }
    public Result<Updated> MarkAsRead(Guid readerId, DateTimeOffset readAtUtc)
    {
        if (!IsParticipant(readerId))
        {
            return ConversationErrors.NotParticipant;
        }
        foreach (var message in _messages)
        {
            if (message.SenderId != readerId)
            {
                message.MarkAsRead(readAtUtc);
            }
        }
        return Result.Updated;
    }
    private bool IsParticipant(Guid senderId) => senderId == BuyerId || senderId == SellerId;
}
