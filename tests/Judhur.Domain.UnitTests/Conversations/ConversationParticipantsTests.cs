using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Conversations;

public sealed class ConversationParticipantsTests
{
    private static readonly Guid BuyerId = ConversationFactory.DefaultBuyerId;
    private static readonly Guid SellerId = ConversationFactory.DefaultSellerId;

    [Fact]
    public void IsParticipant_Buyer_ReturnsTrue()
    {
        var conversation = ConversationFactory.Create();

        Assert.True(conversation.IsParticipant(BuyerId));
    }

    [Fact]
    public void IsParticipant_Seller_ReturnsTrue()
    {
        var conversation = ConversationFactory.Create();

        Assert.True(conversation.IsParticipant(SellerId));
    }

    [Fact]
    public void IsParticipant_Stranger_ReturnsFalse()
    {
        var conversation = ConversationFactory.Create();

        Assert.False(conversation.IsParticipant(Guid.NewGuid()));
    }

    [Fact]
    public void IsParticipant_EmptyId_ReturnsFalse()
    {
        var conversation = ConversationFactory.Create();

        Assert.False(conversation.IsParticipant(Guid.Empty));
    }

    [Fact]
    public void IsParticipant_PropertyId_ReturnsFalse()
    {
        var conversation = ConversationFactory.Create();

        Assert.False(conversation.IsParticipant(conversation.PropertyId));
    }

    [Fact]
    public void OtherParticipantOf_Buyer_ReturnsSeller()
    {
        var conversation = ConversationFactory.Create();

        Assert.Equal(SellerId, conversation.OtherParticipantOf(BuyerId));
    }

    [Fact]
    public void OtherParticipantOf_Seller_ReturnsBuyer()
    {
        var conversation = ConversationFactory.Create();

        Assert.Equal(BuyerId, conversation.OtherParticipantOf(SellerId));
    }
}
