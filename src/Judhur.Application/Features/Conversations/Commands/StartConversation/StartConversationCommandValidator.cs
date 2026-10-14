using FluentValidation;

using Judhur.Domain.Conversations.Messages;

namespace Judhur.Application.Features.Conversations.Commands.StartConversation;

public sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(c => c.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");

        RuleFor(c => c.Message)
            .NotEmpty().WithMessage("نص الرسالة مطلوب.")
            .Must(message => message is null || message.Trim().Length <= Message.MaxContentLength)
            .WithMessage($"لا يمكن أن تتجاوز الرسالة {Message.MaxContentLength} حرف.");
    }
}
