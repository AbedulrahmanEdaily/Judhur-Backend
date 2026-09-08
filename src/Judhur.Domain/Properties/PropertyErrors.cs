using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Properties;

public static class PropertyErrors
{
    public static readonly Error TitleRequired = Error.Validation(
        "PropertyErrors.TitleRequired",
        "العنوان مطلوب");

    public static readonly Error MainImageRequired = Error.Validation(
        "PropertyErrors.MainImageRequired",
        "الصورة الرئيسية مطلوبة");

    public static readonly Error PriceInvalid = Error.Validation(
        "PropertyErrors.PriceInvalid",
        "السعر يجب أن يكون أكبر من صفر");

    public static readonly Error PaymentInvalid = Error.Validation(
        "PropertyErrors.PaymentInvalid",
        "طريقة الدفع غير صالحة");

    public static readonly Error PropertyTypeInvalid = Error.Validation(
        "PropertyErrors.PropertyTypeInvalid",
        "نوع العقار غير صالح");

    public static readonly Error PropertyStatusInvalid = Error.Validation(
        "PropertyErrors.PropertyStatusInvalid",
        "حالة العقار غير صالحة");

    public static readonly Error LandClassificationInvalid = Error.Validation(
        "PropertyErrors.LandClassificationInvalid",
        "تصنيف الأرض غير صالح");

    public static readonly Error LegalStatusInvalid = Error.Validation(
        "PropertyErrors.LegalStatusInvalid",
        "نوع الوثيقة غير صالح");

    public static readonly Error AreaInvalid = Error.Validation(
        "PropertyErrors.AreaInvalid",
        "المساحة يجب أن تكون أكبر من صفر");

    public static readonly Error CityRequired = Error.Validation(
        "PropertyErrors.CityRequired",
        "المدينة مطلوبة");

    public static readonly Error FullAddressRequired = Error.Validation(
        "PropertyErrors.FullAddressRequired",
        "العنوان الكامل مطلوب");

    public static readonly Error OwnershipDocumentRequired = Error.Validation(
        "PropertyErrors.OwnershipDocumentRequired",
        "وثيقة الملكية مطلوبة");

    public static readonly Error SellerRequired = Error.Validation(
        "PropertyErrors.SellerRequired",
        "معرّف البائع مطلوب");

    public static readonly Error LatitudeInvalid = Error.Validation(
        "PropertyErrors.LatitudeInvalid",
        "خط العرض غير صحيح");

    public static readonly Error LongitudeInvalid = Error.Validation(
        "PropertyErrors.LongitudeInvalid",
        "خط الطول غير صحيح");

    public static readonly Error MaxImagesReached = Error.Validation(
        "PropertyErrors.MaxImagesReached",
        "الحد الأقصى للصور هو 10");

    public static readonly Error ImageUrlRequired = Error.Validation(
        "PropertyErrors.ImageUrlRequired",
        "رابط الصورة مطلوب");

    public static readonly Error ImageNotFound = Error.NotFound(
        "PropertyErrors.ImageNotFound",
        "الصورة غير موجودة");

    public static readonly Error CannotRemoveMainImage = Error.Validation(
        "PropertyErrors.CannotRemoveMainImage",
        "لا يمكن حذف الصورة الرئيسية");

    public static readonly Error AlreadyApproved = Error.Conflict(
        "PropertyErrors.AlreadyApproved",
        "العقار مصرح به بالفعل");

    public static readonly Error CannotRejectApprovedProperty = Error.Conflict(
        "PropertyErrors.CannotRejectApprovedProperty",
        "لا يمكن رفض عقار مصرح به");

    public static readonly Error AlreadyDeactivated = Error.Conflict(
        "PropertyErrors.AlreadyDeactivated",
        "العقار معطل بالفعل");

    public static readonly Error AlreadyActive = Error.Conflict(
        "PropertyErrors.AlreadyActive",
        "العقار مفعل بالفعل");

    public static readonly Error CannotReactivateUnapprovedProperty = Error.Conflict(
        "PropertyErrors.CannotReactivateUnapprovedProperty",
        "لا يمكن تفعيل عقار غير مصرح به");

    public static readonly Error CannotUpdateUnapprovedProperty = Error.Conflict(
        "PropertyErrors.CannotUpdateUnapprovedProperty",
        "لا يمكن تحديث حالة عقار غير مصرح به");
}
