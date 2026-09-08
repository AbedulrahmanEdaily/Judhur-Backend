using Judhur.Domain.Common.Results;

namespace Judhur.Domain.Properties.PropertyImages;

public static class PropertyImageErrors
{
    public static readonly Error FileUrlRequired = Error.Validation(
        "PropertyImage.FileUrlRequired",
        "رابط الصورة مطلوب");

    public static readonly Error PublicIdRequired = Error.Validation(
        "PropertyImage.PublicIdRequired",
        "معرف Cloudinary مطلوب");

    public static readonly Error DisplayOrderInvalid = Error.Validation(
        "PropertyImage.DisplayOrderInvalid",
        "ترتيب العرض يجب أن يكون أكبر من صفر");

    public static readonly Error PropertyIdRequired = Error.Validation(
        "PropertyImage.PropertyIdRequired",
        "معرف العقار مطلوب");
}
