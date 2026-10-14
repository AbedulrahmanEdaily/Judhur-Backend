using FluentValidation;

namespace Judhur.Application.Features.Conversations.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryValidator : AbstractValidator<GetConversationMessagesQuery>
{
    public const int MaxLimit = 100;

    public GetConversationMessagesQueryValidator()
    {
        RuleFor(q => q.ConversationId)
            .NotEmpty().WithMessage("معرّف المحادثة مطلوب.");

        RuleFor(q => q.Limit)
            .InclusiveBetween(1, MaxLimit).WithMessage($"عدد الرسائل يجب أن يكون بين 1 و{MaxLimit}.");
    }
}
