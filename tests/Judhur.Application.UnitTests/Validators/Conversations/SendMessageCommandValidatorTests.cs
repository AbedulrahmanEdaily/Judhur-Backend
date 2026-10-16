using FluentValidation.TestHelper;

using Judhur.Application.Features.Conversations.Commands.SendMessage;
using Judhur.Domain.Conversations.Messages;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Conversations;

public sealed class SendMessageCommandValidatorTests
{
    private readonly SendMessageCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new SendMessageCommand(Guid.NewGuid(), "مرحبا")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyConversationId_ReturnsError()
    {
        _validator.TestValidate(new SendMessageCommand(Guid.Empty, "مرحبا"))
            .ShouldHaveValidationErrorFor(c => c.ConversationId)
            .WithErrorMessage("معرّف المحادثة مطلوب.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankContent_ReturnsError(string content)
    {
        _validator.TestValidate(new SendMessageCommand(Guid.NewGuid(), content))
            .ShouldHaveValidationErrorFor(c => c.Content)
            .WithErrorMessage("نص الرسالة مطلوب.");
    }

    [Fact]
    public void Validate_ContentAtMaxLength_HasNoError()
    {
        _validator.TestValidate(new SendMessageCommand(Guid.NewGuid(), Text(Message.MaxContentLength)))
            .ShouldNotHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void Validate_ContentAtMaxLengthAfterTrim_HasNoError()
    {
        _validator.TestValidate(new SendMessageCommand(Guid.NewGuid(), $"  {Text(Message.MaxContentLength)}  "))
            .ShouldNotHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void Validate_ContentOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(new SendMessageCommand(Guid.NewGuid(), Text(Message.MaxContentLength + 1)))
            .ShouldHaveValidationErrorFor(c => c.Content)
            .WithErrorMessage($"لا يمكن أن تتجاوز الرسالة {Message.MaxContentLength} حرف.");
    }
}
