using FluentValidation;

namespace Judhur.Application.Features.Reports.Queries.GetReports;

public sealed class GetReportsQueryValidator : AbstractValidator<GetReportsQuery>
{
    public GetReportsQueryValidator()
    {
        RuleFor(q => q.Status)
            .IsInEnum().WithMessage("حالة البلاغ غير صالحة.");
    }
}
