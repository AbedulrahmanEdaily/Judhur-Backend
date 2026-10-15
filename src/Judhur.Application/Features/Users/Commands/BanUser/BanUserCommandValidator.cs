using FluentValidation;

namespace Judhur.Application.Features.Users.Commands.BanUser;

public sealed class BanUserCommandValidator : AbstractValidator<BanUserCommand>
{
    public const int MaxReasonLength = 500;

    public BanUserCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty().WithMessage("معرّف المستخدم مطلوب.");

        RuleFor(c => c.Reason)
            .NotEmpty().WithMessage("سبب الحظر مطلوب.")
            .Must(reason => reason is null || reason.Trim().Length <= MaxReasonLength)
            .WithMessage($"لا يمكن أن يتجاوز سبب الحظر {MaxReasonLength} حرف.");
    }
}
