using FluentValidation;

using Judhur.Domain.Properties;

namespace Judhur.Application.Features.Properties.Commands.RejectProperty;

public sealed class RejectPropertyCommandValidator : AbstractValidator<RejectPropertyCommand>
{
    public RejectPropertyCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");

        RuleFor(p => p.RejectionReason)
            .NotEmpty().WithMessage("سبب الرفض مطلوب.")
            .MaximumLength(Property.MaxRejectionReasonLength)
            .WithMessage($"لا يمكن أن يتجاوز سبب الرفض {Property.MaxRejectionReasonLength} حرف.");
    }
}