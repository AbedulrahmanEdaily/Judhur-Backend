using Judhur.Domain.Common.Results;
using Judhur.Domain.Conversations;
using Judhur.Domain.Conversations.Messages;

namespace Judhur.Tests.Common.Factories;

public static class ConversationFactory
{
    public const string DefaultMessageContent = "مرحبًا، هل العقار ما زال متاحًا؟";

    public static readonly Guid DefaultBuyerId = Guid.Parse("0199a1b2-0000-7000-8000-000000000002");
    public static readonly Guid DefaultSellerId = PropertyFactory.DefaultSellerId;
    public static readonly Guid DefaultPropertyId = Guid.Parse("0199a1b2-0000-7000-8000-0000000000b1");

    public static Result<Conversation> CreateResult(
        Guid? buyerId = null,
        Guid? sellerId = null,
        Guid? propertyId = null,
        Guid? id = null)
        => Conversation.Create(
            id ?? Guid.NewGuid(),
            buyerId ?? DefaultBuyerId,
            sellerId ?? DefaultSellerId,
            propertyId ?? DefaultPropertyId);

    public static Conversation Create(
        Guid? buyerId = null,
        Guid? sellerId = null,
        Guid? propertyId = null,
        Guid? id = null)
        => CreateResult(buyerId, sellerId, propertyId, id).GetValueOrThrow();

    public static Message AddMessage(
        Conversation conversation,
        Guid senderId,
        DateTimeOffset? sentAtUtc = null,
        string content = DefaultMessageContent,
        Guid? messageId = null)
        => conversation.AddMessage(
            messageId ?? Guid.NewGuid(),
            senderId,
            content,
            sentAtUtc ?? TestClock.UtcNow).GetValueOrThrow();
}
