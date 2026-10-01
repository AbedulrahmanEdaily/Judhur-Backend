using FluentValidation;

namespace Judhur.Application.Features.Notifications.Commands.MarkAsRead;

public sealed class MarkAsReadCommandValidator : AbstractValidator<MarkAsReadCommand>
{
    public MarkAsReadCommandValidator()
    {
        RuleFor(c => c.NotificationId)
            .NotEmpty().WithMessage("معرّف الإشعار مطلوب.");
    }
}