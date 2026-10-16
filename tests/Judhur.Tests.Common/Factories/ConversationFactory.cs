using Judhur.Domain.Common.Results;
using Judhur.Domain.Conversations;

namespace Judhur.Tests.Common.Factories;

public static class ConversationFactory
{
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
}
