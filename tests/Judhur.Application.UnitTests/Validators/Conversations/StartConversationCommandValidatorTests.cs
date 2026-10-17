using FluentValidation.TestHelper;

using Judhur.Application.Features.Conversations.Commands.StartConversation;
using Judhur.Domain.Conversations.Messages;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Conversations;

public sealed class StartConversationCommandValidatorTests
{
    private readonly StartConversationCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new StartConversationCommand(Guid.NewGuid(), "هل العقار متاح؟")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyPropertyId_ReturnsError()
    {
        _validator.TestValidate(new StartConversationCommand(Guid.Empty, "مرحبا"))
            .ShouldHaveValidationErrorFor(c => c.PropertyId)
            .WithErrorMessage("معرّف العقار مطلوب.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("\t ")]
    public void Validate_BlankMessage_ReturnsError(string message)
    {
        _validator.TestValidate(new StartConversationCommand(Guid.NewGuid(), message))
            .ShouldHaveValidationErrorFor(c => c.Message)
            .WithErrorMessage("نص الرسالة مطلوب.");
    }

    [Fact]
    public void Validate_MessageAtMaxLengthAfterTrim_HasNoError()
    {
        _validator.TestValidate(new StartConversationCommand(Guid.NewGuid(), $" {Text(Message.MaxContentLength)} "))
            .ShouldNotHaveValidationErrorFor(c => c.Message);
    }

    [Fact]
    public void Validate_MessageOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(new StartConversationCommand(Guid.NewGuid(), Text(Message.MaxContentLength + 1)))
            .ShouldHaveValidationErrorFor(c => c.Message);
    }
}
