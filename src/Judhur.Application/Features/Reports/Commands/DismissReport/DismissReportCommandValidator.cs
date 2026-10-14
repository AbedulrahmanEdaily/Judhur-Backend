using FluentValidation;

using Judhur.Domain.Reports;

namespace Judhur.Application.Features.Reports.Commands.DismissReport;

public sealed class DismissReportCommandValidator : AbstractValidator<DismissReportCommand>
{
    public DismissReportCommandValidator()
    {
        RuleFor(r => r.ReportId)
            .NotEmpty().WithMessage("معرّف البلاغ مطلوب.");

        RuleFor(r => r.AdminNote)
            .MaximumLength(Report.MaxAdminNoteLength).WithMessage($"لا يمكن أن تتجاوز ملاحظة المشرف {Report.MaxAdminNoteLength} حرف.");
    }
}
