using Judhur.Domain.Common.Result;

namespace Judhur.Domain.PropertyErrors;

public static class PropertyErrors
{
    public static Error TitleRequired = Error.Validation(
        "PropertyErrors.TitleRequired",
        "العنوان مطلوب");
    public static Error MainImageRequired = Error.Validation(
        "PropertyErrors.TitleRequired",
        "الصوره الرئيسية مطلوبة");

    public static Error PriceInvalid = Error.Validation(
        "PropertyErrors.PriceInvalid",
        "السعر يجب أن يكون أكبر من صفر");
    public static Error PaymentInvalid => Error.Validation(
        "PropertyErrors.PaymentInvalid",
        "طريقة الدفع غير صالحة");
    public static Error PropertyTypeInvalid => Error.Validation(
        "PropertyErrors.PropertyTypeInvalid",
        "نوع العقار غير صالح");
    public static Error PropertyStatusInvalid => Error.Validation(
        "PropertyErrors.PropertyStatusInvalid",
        "حالة العقار غير صالح");
    public static Error LandClassificationInvalid => Error.Validation(
        "PropertyErrors.LandClassificationInvalid",
        "تصنيف العقار غير صالح");
    public static Error LegalStatusInvalid => Error.Validation(
        "PropertyErrors.LegalStatusInvalid",
        "نوع الوثيقة غير صالح");
    public static Error AreaInvalid = Error.Validation(
        "PropertyErrors.AreaInvalid",
        "المساحة يجب أن تكون أكبر من صفر");

    public static Error CityRequired = Error.Validation(
        "PropertyErrors.CityRequired",
        "المدينة مطلوبة");

    public static Error FullAddressRequired = Error.Validation(
        "PropertyErrors.FullAddressRequired",
        "العنوان الكامل مطلوب");

    public static Error OwnershipDocumentRequired = Error.Validation(
        "PropertyErrors.OwnershipDocumentRequired",
        "وثيقة الملكية مطلوبة");

    public static Error LatitudeInvalid = Error.Validation(
        "PropertyErrors.LatitudeInvalid",
        "خط العرض غير صحيح");

    public static Error LongitudeInvalid = Error.Validation(
        "PropertyErrors.LongitudeInvalid",
        "خط الطول غير صحيح");

    public static Error MaxImagesReached = Error.Validation(
        "PropertyErrors.MaxImagesReached",
        "الحد الأقصى للصور هو 10");

    public static Error ImageUrlRequired = Error.Validation(
        "PropertyErrors.ImageUrlRequired",
        "رابط الصورة مطلوب");

    public static Error ImageNotFound = Error.NotFound(
        "PropertyErrors.ImageNotFound",
        "الصورة غير موجودة");

    public static Error CannotRemoveMainImage = Error.Validation(
        "PropertyErrors.CannotRemoveMainImage",
        "لا يمكن حذف الصورة الرئيسية");

    public static Error AlreadyApproved = Error.Conflict(
        "PropertyErrors.AlreadyApproved",
        "العقار مصرح به بالفعل");

    public static Error CannotRejectApprovedProperty = Error.Conflict(
        "PropertyErrors.CannotRejectApprovedProperty",
        "لا يمكن رفض عقار مصرح به");

    public static Error AlreadyDeactivated = Error.Conflict(
        "PropertyErrors.AlreadyDeactivated",
        "العقار معطل بالفعل");

    public static Error AlreadyActive = Error.Conflict(
        "PropertyErrors.AlreadyActive",
        "العقار مفعل بالفعل");

    public static Error CannotReactivateUnapprovedProperty = Error.Conflict(
        "PropertyErrors.CannotReactivateUnapprovedProperty",
        "لا يمكن تفعيل عقار غير مصرح به");

    public static Error CannotUpdateUnapprovedProperty = Error.Conflict(
        "PropertyErrors.CannotUpdateUnapprovedProperty",
        "لا يمكن تحديث حالة عقار غير مصرح به");
}