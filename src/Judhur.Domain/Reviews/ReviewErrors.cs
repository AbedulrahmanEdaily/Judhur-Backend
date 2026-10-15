using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Reviews;

public static class ReviewErrors
{
    public static readonly Error SellerIdRequired = Error.Validation("ReviewErrors.SellerIdRequired", "البائع مطلوب");
    public static readonly Error ReviewerIdRequired = Error.Validation("ReviewErrors.ReviewerIdRequired", "المقيِّم مطلوب");
    public static readonly Error CannotReviewSelf = Error.Conflict("ReviewErrors.CannotReviewSelf", "لا تستطيع تقييم نفسك");
    public static readonly Error InvalidRating = Error.Validation("ReviewErrors.InvalidRating", "التقييم يجب أن يكون من 0.5 إلى 5 بخطوات 0.5");
    public static readonly Error CommentTooLong = Error.Validation("ReviewErrors.CommentTooLong", $"لا يمكن أن يتجاوز التعليق {Review.MaxCommentLength} حرف");
    public static readonly Error NotFound = Error.NotFound("ReviewErrors.NotFound", "التقييم غير موجود");
    public static readonly Error NotEligible = Error.Conflict("ReviewErrors.NotEligible", "يمكنك تقييم البائع بعد التواصل معه ورده على رسالتك");
    public static readonly Error DuplicateReview = Error.Conflict("ReviewErrors.DuplicateReview", "لقد قيّمت هذا البائع مسبقًا، يمكنك تعديل تقييمك");
}