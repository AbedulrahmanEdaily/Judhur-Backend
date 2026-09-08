using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Reviews;

public static class ReviewErrors
{
    public static readonly Error SellerIdRequired = Error.Validation("ReviewErrors.SellerIdRequired", "البائع مطلوب");
    public static readonly Error ReviewerIdRequired = Error.Validation("ReviewErrors.ReviewerIdRequired", "المقيم مطلوب");
    public static readonly Error CannotReviewSelf = Error.Conflict("ReviewErrors.CannotReviewSelf", "لا تستطيع تقييم نفسك");
    public static readonly Error InvalidRating = Error.Validation("ReviewErrors.InvalidRating", "التقييم غير صحيح");
    public static readonly Error DuplicateReview = Error.Conflict("ReviewErrors.DuplicateReview", "لا تستطيع تقييم نفس الشخص مرة أخرى");
}