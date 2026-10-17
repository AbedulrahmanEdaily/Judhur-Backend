using Judhur.Domain.Conversations;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Conversations;

public sealed class ConversationCreateTests
{
    [Fact]
    public void Create_ValidParticipants_ReturnsSuccess()
    {
        var result = ConversationFactory.CreateResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ValidParticipants_CopiesData()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();

        var conversation = ConversationFactory.Create(buyerId, sellerId, propertyId);

        Assert.Equal(buyerId, conversation.BuyerId);
        Assert.Equal(sellerId, conversation.SellerId);
        Assert.Equal(propertyId, conversation.PropertyId);
    }

    [Fact]
    public void Create_ValidParticipants_StartsWithoutMessages()
    {
        var conversation = ConversationFactory.Create();

        Assert.Empty(conversation.Messages);
        Assert.Null(conversation.LastMessageAtUtc);
    }

    [Fact]
    public void Create_GivenId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var conversation = ConversationFactory.Create(id: id);

        Assert.Equal(id, conversation.Id);
    }

    [Fact]
    public void Create_EmptyId_GeneratesNewId()
    {
        var conversation = ConversationFactory.Create(id: Guid.Empty);

        Assert.NotEqual(Guid.Empty, conversation.Id);
    }

    [Fact]
    public void Create_EmptyBuyer_ReturnsBuyerIdRequired()
    {
        var result = ConversationFactory.CreateResult(buyerId: Guid.Empty);

        Assert.Equal(ConversationErrors.BuyerIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptySeller_ReturnsSellerIdRequired()
    {
        var result = ConversationFactory.CreateResult(sellerId: Guid.Empty);

        Assert.Equal(ConversationErrors.SellerIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptyProperty_ReturnsPropertyIdRequired()
    {
        var result = ConversationFactory.CreateResult(propertyId: Guid.Empty);

        Assert.Equal(ConversationErrors.PropertyIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_BuyerIsSeller_ReturnsCannotStartConversationWithSelf()
    {
        var userId = Guid.NewGuid();

        var result = ConversationFactory.CreateResult(userId, userId);

        Assert.Equal(ConversationErrors.CannotStartConversationWithSelf.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_AllIdsEmpty_ReturnsBuyerIdRequiredFirst()
    {
        var result = ConversationFactory.CreateResult(Guid.Empty, Guid.Empty, Guid.Empty);

        Assert.Equal(ConversationErrors.BuyerIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptySellerAndProperty_ReturnsSellerIdRequiredFirst()
    {
        var result = ConversationFactory.CreateResult(sellerId: Guid.Empty, propertyId: Guid.Empty);

        Assert.Equal(ConversationErrors.SellerIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_BuyerIsSellerWithEmptyProperty_ReturnsPropertyIdRequiredFirst()
    {
        var userId = Guid.NewGuid();

        var result = ConversationFactory.CreateResult(userId, userId, Guid.Empty);

        Assert.Equal(ConversationErrors.PropertyIdRequired.Code, result.TopError.Code);
    }
}
