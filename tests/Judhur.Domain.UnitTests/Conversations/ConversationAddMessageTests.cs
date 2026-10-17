using Judhur.Domain.Conversations;
using Judhur.Domain.Conversations.Messages;
using Judhur.Tests.Common;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Conversations;

public sealed class ConversationAddMessageTests
{
    private static readonly Guid BuyerId = ConversationFactory.DefaultBuyerId;
    private static readonly Guid SellerId = ConversationFactory.DefaultSellerId;
    private static readonly DateTimeOffset SentAt = TestClock.UtcNow;

    [Fact]
    public void AddMessage_FromBuyer_ReturnsSuccess()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.AddMessage(Guid.NewGuid(), BuyerId, ConversationFactory.DefaultMessageContent, SentAt);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddMessage_FromSeller_ReturnsSuccess()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.AddMessage(Guid.NewGuid(), SellerId, "نعم، ما زال متاحًا", SentAt);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddMessage_ValidMessage_ReturnsMessageWithData()
    {
        var conversation = ConversationFactory.Create();
        var messageId = Guid.NewGuid();

        var message = conversation.AddMessage(messageId, BuyerId, ConversationFactory.DefaultMessageContent, SentAt).GetValueOrThrow();

        Assert.Equal(messageId, message.Id);
        Assert.Equal(conversation.Id, message.ConversationId);
        Assert.Equal(BuyerId, message.SenderId);
        Assert.Equal(ConversationFactory.DefaultMessageContent, message.Content);
        Assert.Equal(SentAt, message.SentAtUtc);
    }

    [Fact]
    public void AddMessage_ValidMessage_StartsUnread()
    {
        var conversation = ConversationFactory.Create();

        var message = ConversationFactory.AddMessage(conversation, BuyerId);

        Assert.False(message.IsRead);
        Assert.Null(message.ReadAtUtc);
    }

    [Fact]
    public void AddMessage_ValidMessage_AddsItToConversation()
    {
        var conversation = ConversationFactory.Create();

        var message = ConversationFactory.AddMessage(conversation, BuyerId);

        Assert.Same(message, Assert.Single(conversation.Messages));
    }

    [Fact]
    public void AddMessage_SeveralMessages_KeepsInsertionOrder()
    {
        var conversation = ConversationFactory.Create();

        var first = ConversationFactory.AddMessage(conversation, BuyerId, SentAt);
        var second = ConversationFactory.AddMessage(conversation, SellerId, SentAt.AddMinutes(1));
        var third = ConversationFactory.AddMessage(conversation, BuyerId, SentAt.AddMinutes(2));

        Assert.Equal([first.Id, second.Id, third.Id], conversation.Messages.Select(m => m.Id));
    }

    [Fact]
    public void AddMessage_EmptyMessageId_GeneratesMessageId()
    {
        var conversation = ConversationFactory.Create();

        var message = ConversationFactory.AddMessage(conversation, BuyerId, messageId: Guid.Empty);

        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void AddMessage_ConversationCreatedWithEmptyId_LinksToGeneratedId()
    {
        var conversation = ConversationFactory.Create(id: Guid.Empty);

        var message = ConversationFactory.AddMessage(conversation, BuyerId);

        Assert.Equal(conversation.Id, message.ConversationId);
        Assert.NotEqual(Guid.Empty, message.ConversationId);
    }

    [Fact]
    public void AddMessage_FromStranger_ReturnsNotParticipant()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.AddMessage(Guid.NewGuid(), Guid.NewGuid(), ConversationFactory.DefaultMessageContent, SentAt);

        Assert.Equal(ConversationErrors.NotParticipant.Code, result.TopError.Code);
    }

    [Fact]
    public void AddMessage_FromEmptySender_ReturnsNotParticipant()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.AddMessage(Guid.NewGuid(), Guid.Empty, ConversationFactory.DefaultMessageContent, SentAt);

        Assert.Equal(ConversationErrors.NotParticipant.Code, result.TopError.Code);
    }

    [Fact]
    public void AddMessage_FromStrangerWithBlankContent_ReturnsNotParticipantFirst()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.AddMessage(Guid.NewGuid(), Guid.NewGuid(), "", SentAt);

        Assert.Equal(ConversationErrors.NotParticipant.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\n\t ")]
    public void AddMessage_BlankContent_ReturnsContentRequired(string content)
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.AddMessage(Guid.NewGuid(), BuyerId, content, SentAt);

        Assert.Equal(MessageErrors.ContentRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void AddMessage_NullContent_ReturnsContentRequired()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.AddMessage(Guid.NewGuid(), BuyerId, null!, SentAt);

        Assert.Equal(MessageErrors.ContentRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void AddMessage_ContentWithSurroundingWhitespace_StoresTrimmedContent()
    {
        var conversation = ConversationFactory.Create();

        var message = ConversationFactory.AddMessage(conversation, BuyerId, content: $"  {ConversationFactory.DefaultMessageContent}\n");

        Assert.Equal(ConversationFactory.DefaultMessageContent, message.Content);
    }

    [Fact]
    public void AddMessage_ContentAtLimit_ReturnsSuccess()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.AddMessage(Guid.NewGuid(), BuyerId, new string('ر', Message.MaxContentLength), SentAt);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddMessage_ContentAtLimitAfterTrimming_ReturnsSuccess()
    {
        var conversation = ConversationFactory.Create();
        var content = new string('ر', Message.MaxContentLength);

        var message = ConversationFactory.AddMessage(conversation, BuyerId, content: $"   {content}   ");

        Assert.Equal(content, message.Content);
    }

    [Fact]
    public void AddMessage_ContentOverLimit_ReturnsContentTooLong()
    {
        var conversation = ConversationFactory.Create();

        var result = conversation.AddMessage(Guid.NewGuid(), BuyerId, new string('ر', Message.MaxContentLength + 1), SentAt);

        Assert.Equal(MessageErrors.ContentTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void AddMessage_Invalid_DoesNotAddMessageOrTouchLastMessageTime()
    {
        var conversation = ConversationFactory.Create();
        ConversationFactory.AddMessage(conversation, BuyerId, SentAt);

        conversation.AddMessage(Guid.NewGuid(), SellerId, " ", SentAt.AddHours(1));
        conversation.AddMessage(Guid.NewGuid(), Guid.NewGuid(), "رسالة", SentAt.AddHours(2));

        Assert.Single(conversation.Messages);
        Assert.Equal(SentAt, conversation.LastMessageAtUtc);
    }

    [Fact]
    public void AddMessage_FirstMessage_SetsLastMessageTime()
    {
        var conversation = ConversationFactory.Create();

        ConversationFactory.AddMessage(conversation, BuyerId, SentAt);

        Assert.Equal(SentAt, conversation.LastMessageAtUtc);
    }

    [Fact]
    public void AddMessage_LaterMessage_AdvancesLastMessageTime()
    {
        var conversation = ConversationFactory.Create();
        ConversationFactory.AddMessage(conversation, BuyerId, SentAt);

        ConversationFactory.AddMessage(conversation, SellerId, SentAt.AddMinutes(10));

        Assert.Equal(SentAt.AddMinutes(10), conversation.LastMessageAtUtc);
    }

    [Fact]
    public void AddMessage_EarlierMessage_KeepsLatestLastMessageTime()
    {
        var conversation = ConversationFactory.Create();
        ConversationFactory.AddMessage(conversation, BuyerId, SentAt);

        ConversationFactory.AddMessage(conversation, SellerId, SentAt.AddMinutes(-10));

        Assert.Equal(SentAt, conversation.LastMessageAtUtc);
        Assert.Equal(2, conversation.Messages.Count);
    }

    [Fact]
    public void AddMessage_SameTimeAsLastMessage_KeepsLastMessageTime()
    {
        var conversation = ConversationFactory.Create();
        ConversationFactory.AddMessage(conversation, BuyerId, SentAt);

        ConversationFactory.AddMessage(conversation, SellerId, SentAt);

        Assert.Equal(SentAt, conversation.LastMessageAtUtc);
    }
}
