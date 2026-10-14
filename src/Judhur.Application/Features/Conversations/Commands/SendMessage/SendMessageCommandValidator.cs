using FluentValidation;

using Judhur.Domain.Conversations.Messages;

namespace Judhur.Application.Features.Conversations.Commands.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(c => c.ConversationId)
            .NotEmpty().WithMessage("معرّف المحادثة مطلوب.");

        RuleFor(c => c.Content)
            .NotEmpty().WithMessage("نص الرسالة مطلوب.")
            .Must(content => content is null || content.Trim().Length <= Message.MaxContentLength)
            .WithMessage($"لا يمكن أن تتجاوز الرسالة {Message.MaxContentLength} حرف.");
    }
}
