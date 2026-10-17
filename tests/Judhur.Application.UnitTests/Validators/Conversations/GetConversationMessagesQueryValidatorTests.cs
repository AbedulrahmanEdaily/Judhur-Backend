using FluentValidation.TestHelper;

using Judhur.Application.Features.Conversations.Queries.GetConversationMessages;

namespace Judhur.Application.UnitTests.Validators.Conversations;

public sealed class GetConversationMessagesQueryValidatorTests
{
    private readonly GetConversationMessagesQueryValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(GetConversationMessagesQueryValidator.MaxLimit)]
    public void Validate_LimitWithinRange_HasNoErrors(int limit)
    {
        _validator.TestValidate(new GetConversationMessagesQuery(Guid.NewGuid(), null, limit)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithBeforeCursor_HasNoErrors()
    {
        _validator.TestValidate(new GetConversationMessagesQuery(Guid.NewGuid(), Guid.NewGuid(), 20)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(GetConversationMessagesQueryValidator.MaxLimit + 1)]
    public void Validate_LimitOutOfRange_ReturnsError(int limit)
    {
        _validator.TestValidate(new GetConversationMessagesQuery(Guid.NewGuid(), null, limit))
            .ShouldHaveValidationErrorFor(q => q.Limit)
            .WithErrorMessage("عدد الرسائل يجب أن يكون بين 1 و100.");
    }

    [Fact]
    public void Validate_EmptyConversationId_ReturnsError()
    {
        _validator.TestValidate(new GetConversationMessagesQuery(Guid.Empty, null, 20))
            .ShouldHaveValidationErrorFor(q => q.ConversationId)
            .WithErrorMessage("معرّف المحادثة مطلوب.");
    }
}
