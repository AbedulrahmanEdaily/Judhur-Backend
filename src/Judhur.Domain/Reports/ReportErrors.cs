using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Reports;

public static class ReportErrors
{
    public static readonly Error ReporterIdRequired = Error.Validation("ReportErrors.ReporterIdRequired", "المبلغ مطلوب");
    public static readonly Error PropertyIdRequired = Error.Validation("ReportErrors.PropertyIdRequired", "العقار مطلوب");
    public static readonly Error InvalidReason = Error.Validation("ReportErrors.InvalidReason", "سبب البلاغ غير صالح");
    public static readonly Error DetailsRequiredForOtherReason = Error.Validation("ReportErrors.DetailsRequiredForOtherReason", "يجب توضيح سبب البلاغ عند اختيار (أخرى)");
    public static readonly Error AdminIdRequired = Error.Validation("ReportErrors.AdminIdRequired", "معرف المشرف مطلوب");
    public static readonly Error AlreadyReviewed = Error.Conflict("ReportErrors.AlreadyReviewed", "تمت مراجعة هذا البلاغ مسبقا");
    public static readonly Error CannotReportOwnProperty = Error.Conflict("ReportErrors.CannotReportOwnProperty", "لا تستطيع الابلاغ عن عقارك");
    public static readonly Error DuplicateReport = Error.Conflict("ReportErrors.DuplicateReport", "لقد ابلغت عن هذا العقار مسبقا");
}
