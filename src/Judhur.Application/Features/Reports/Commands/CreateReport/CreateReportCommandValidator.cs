using FluentValidation;

using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;

namespace Judhur.Application.Features.Reports.Commands.CreateReport;

public sealed class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(r => r.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");

        RuleFor(r => r.Reason)
            .IsInEnum().WithMessage("سبب البلاغ غير صالح.");

        RuleFor(r => r.Details)
            .NotEmpty().WithMessage("يجب توضيح سبب البلاغ عند اختيار (أخرى).")
            .When(r => r.Reason == ReportReason.Other);

        RuleFor(r => r.Details)
            .Must(details => details is null || details.Trim().Length <= Report.MaxDetailsLength)
            .WithMessage($"لا يمكن أن تتجاوز تفاصيل البلاغ {Report.MaxDetailsLength} حرف.");
    }
}
