using Judhur.Domain.Conversations;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Conversations;

public sealed class ConversationMarkAsReadTests
{
    private static readonly Guid BuyerId = ConversationFactory.DefaultBuyerId;
    private static readonly Guid SellerId = ConversationFactory.DefaultSellerId;
    private static readonly DateTimeOffset ReadAt = TestClock.UtcNow.AddHours(1);

    [Fact]
    public void MarkAsRead_ByBuyer_MarksSellerMessagesAsRead()
    {
        var conversation = ConversationFactory.Create();
        var fromSeller = ConversationFactory.AddMessage(conversation, SellerId);

        var result = conversation.MarkAsRead(BuyerId, ReadAt);

        Assert.True(result.IsSuccess);
        Assert.True(fromSeller.IsRead);
        Assert.Equal(ReadAt, fromSeller.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_ByBuyer_LeavesBuyerMessagesUnread()
    {
        var conversation = ConversationFactory.Create();
        var fromBuyer = ConversationFactory.AddMessage(conversation, BuyerId);
        ConversationFactory.AddMessage(conversation, SellerId);

        conversation.MarkAsRead(BuyerId, ReadAt);

        Assert.False(fromBuyer.IsRead);
        Assert.Null(fromBuyer.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_BySeller_MarksOnlyBuyerMessagesAsRead()
    {
        var conversation = ConversationFactory.Create();
        var fromBuyer = ConversationFactory.AddMessage(conversation, BuyerId);
        var fromSeller = ConversationFactory.AddMessage(conversation, SellerId);

        conversation.MarkAsRead(SellerId, ReadAt);

        Assert.True(fromBuyer.IsRead);
        Assert.False(fromSeller.IsRead);
    }

    [Fact]
    public void MarkAsRead_SeveralMessages_MarksAllOtherPartyMessages()
    {
        var conversation = ConversationFactory.Create();
        ConversationFactory.AddMessage(conversation, SellerId, TestClock.UtcNow);
        ConversationFactory.AddMessage(conversation, SellerId, TestClock.UtcNow.AddMinutes(1));
        ConversationFactory.AddMessage(conversation, SellerId, TestClock.UtcNow.AddMinutes(2));

        conversation.MarkAsRead(BuyerId, ReadAt);

        Assert.All(conversation.Messages, m => Assert.Equal(ReadAt, m.ReadAtUtc));
    }

    [Fact]
    public void MarkAsRead_NoMessages_ReturnsSuccess()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.MarkAsRead(BuyerId, ReadAt);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void MarkAsRead_AlreadyRead_KeepsFirstReadTime()
    {
        var conversation = ConversationFactory.Create();
        var fromSeller = ConversationFactory.AddMessage(conversation, SellerId);
        conversation.MarkAsRead(BuyerId, ReadAt).ThrowIfError();

        conversation.MarkAsRead(BuyerId, ReadAt.AddHours(1));

        Assert.Equal(ReadAt, fromSeller.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_NewMessageAfterRead_MarksOnlyNewMessageWithNewTime()
    {
        var conversation = ConversationFactory.Create();
        var first = ConversationFactory.AddMessage(conversation, SellerId);
        conversation.MarkAsRead(BuyerId, ReadAt).ThrowIfError();
        var second = ConversationFactory.AddMessage(conversation, SellerId, ReadAt.AddMinutes(5));
        var secondReadAt = ReadAt.AddMinutes(10);

        conversation.MarkAsRead(BuyerId, secondReadAt);

        Assert.Equal(ReadAt, first.ReadAtUtc);
        Assert.Equal(secondReadAt, second.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_ByStranger_ReturnsNotParticipant()
    {
        var conversation = ConversationFactory.Create();
        ConversationFactory.AddMessage(conversation, SellerId);

        var result = conversation.MarkAsRead(Guid.NewGuid(), ReadAt);

        Assert.Equal(ConversationErrors.NotParticipant.Code, result.TopError.Code);
    }

    [Fact]
    public void MarkAsRead_ByStranger_LeavesAllMessagesUnread()
    {
        var conversation = ConversationFactory.Create();
        ConversationFactory.AddMessage(conversation, SellerId);
        ConversationFactory.AddMessage(conversation, BuyerId);

        conversation.MarkAsRead(Guid.NewGuid(), ReadAt);

        Assert.All(conversation.Messages, m => Assert.False(m.IsRead));
    }

    [Fact]
    public void MarkAsRead_ByEmptyReader_ReturnsNotParticipant()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.MarkAsRead(Guid.Empty, ReadAt);

        Assert.Equal(ConversationErrors.NotParticipant.Code, result.TopError.Code);
    }

    [Fact]
    public void MarkAsRead_ByBuyer_DoesNotChangeLastMessageTime()
    {
        var conversation = ConversationFactory.Create();
        ConversationFactory.AddMessage(conversation, SellerId, TestClock.UtcNow);

        conversation.MarkAsRead(BuyerId, ReadAt);

        Assert.Equal(TestClock.UtcNow, conversation.LastMessageAtUtc);
    }
}
