using FluentValidation;

using Judhur.Application.Common;

namespace Judhur.Application.Features.Properties.Commands.ApproveProperty;

public sealed class ApprovePropertyCommandValidator : AbstractValidator<ApprovePropertyCommand>
{
    public ApprovePropertyCommandValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");

        RuleFor(p => p.RowVersion)
            .Must(value => RowVersions.TryParse(value, out _))
            .WithMessage("نسخة العقار غير صالحة، حدّث صفحة المراجعة.");
    }
}
