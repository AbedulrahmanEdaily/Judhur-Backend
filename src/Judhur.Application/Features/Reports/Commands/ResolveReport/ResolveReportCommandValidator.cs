using FluentValidation;

using Judhur.Domain.Properties;
using Judhur.Domain.Reports;

namespace Judhur.Application.Features.Reports.Commands.ResolveReport;

public sealed class ResolveReportCommandValidator : AbstractValidator<ResolveReportCommand>
{
    public ResolveReportCommandValidator()
    {
        RuleFor(r => r.ReportId)
            .NotEmpty().WithMessage("معرّف البلاغ مطلوب.");

        RuleFor(r => r.AdminNote)
            .MaximumLength(Report.MaxAdminNoteLength).WithMessage($"لا يمكن أن تتجاوز ملاحظة المشرف {Report.MaxAdminNoteLength} حرف.");

        RuleFor(r => r.SuspensionReason)
            .NotEmpty().WithMessage("سبب إيقاف العقار مطلوب.")
            .MaximumLength(Property.MaxRejectionReasonLength).WithMessage($"لا يمكن أن يتجاوز سبب الإيقاف {Property.MaxRejectionReasonLength} حرف.")
            .When(r => r.SuspendProperty);
    }
}
