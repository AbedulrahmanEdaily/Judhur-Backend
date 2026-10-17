using FluentValidation.TestHelper;

using Judhur.Application.Features.Properties.Commands.CreateProperty;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Properties;

public sealed class CreatePropertyCommandValidatorTests
{
    private static readonly CreatePropertyCommand Valid = new(
        "شقة مميزة في رام الله",
        "شقة واسعة",
        250_000m,
        PaymentType.Cash,
        PropertyType.Apartment,
        PropertyStatus.ForSale,
        150m,
        "رام الله",
        "الماصيون",
        "رام الله، الماصيون",
        31.9038,
        35.2034,
        LandClassification.A,
        LegalStatus.Tabo);

    private readonly CreatePropertyCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OptionalFieldsNull_HasNoErrors()
    {
        _validator.TestValidate(Valid with { Description = null, Region = null }).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_BlankTitle_ReturnsError(string title)
    {
        _validator.TestValidate(Valid with { Title = title })
            .ShouldHaveValidationErrorFor(c => c.Title)
            .WithErrorMessage("العنوان مطلوب.");
    }

    [Fact]
    public void Validate_TitleAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { Title = Text(Property.MaxTitleLength) }).ShouldNotHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Validate_TitleOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { Title = Text(Property.MaxTitleLength + 1) }).ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Validate_DescriptionAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { Description = Text(Property.MaxDescriptionLength) }).ShouldNotHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void Validate_DescriptionOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { Description = Text(Property.MaxDescriptionLength + 1) }).ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Validate_NonPositivePrice_ReturnsError(string price)
    {
        _validator.TestValidate(Valid with { Price = decimal.Parse(price) })
            .ShouldHaveValidationErrorFor(c => c.Price)
            .WithErrorMessage("السعر يجب أن يكون أكبر من صفر.");
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("1.5")]
    [InlineData("1000000000000")]
    public void Validate_PriceWithinRange_HasNoError(string price)
    {
        _validator.TestValidate(Valid with { Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture) })
            .ShouldNotHaveValidationErrorFor(c => c.Price);
    }

    [Fact]
    public void Validate_PriceOverMax_ReturnsError()
    {
        _validator.TestValidate(Valid with { Price = Property.MaxPrice + 0.01m })
            .ShouldHaveValidationErrorFor(c => c.Price)
            .WithErrorMessage("السعر أكبر من الحد المسموح.");
    }

    [Fact]
    public void Validate_PriceWithThreeDecimals_ReturnsError()
    {
        _validator.TestValidate(Valid with { Price = 100.005m })
            .ShouldHaveValidationErrorFor(c => c.Price)
            .WithErrorMessage("السعر يقبل خانتين عشريتين كحد أقصى.");
    }

    [Fact]
    public void Validate_UndefinedPaymentType_ReturnsError()
    {
        _validator.TestValidate(Valid with { PaymentType = (PaymentType)99 })
            .ShouldHaveValidationErrorFor(c => c.PaymentType)
            .WithErrorMessage("طريقة الدفع غير صالحة.");
    }

    [Fact]
    public void Validate_UndefinedPropertyType_ReturnsError()
    {
        _validator.TestValidate(Valid with { PropertyType = (PropertyType)99 })
            .ShouldHaveValidationErrorFor(c => c.PropertyType)
            .WithErrorMessage("نوع العقار غير صالح.");
    }

    [Theory]
    [InlineData(PropertyStatus.ForSale)]
    [InlineData(PropertyStatus.ForRent)]
    public void Validate_ListingStatus_HasNoError(PropertyStatus status)
    {
        _validator.TestValidate(Valid with { PropertyStatus = status }).ShouldNotHaveValidationErrorFor(c => c.PropertyStatus);
    }

    [Theory]
    [InlineData(PropertyStatus.Sold)]
    [InlineData(PropertyStatus.Rented)]
    [InlineData((PropertyStatus)99)]
    public void Validate_NonListingStatus_ReturnsError(PropertyStatus status)
    {
        _validator.TestValidate(Valid with { PropertyStatus = status })
            .ShouldHaveValidationErrorFor(c => c.PropertyStatus)
            .WithErrorMessage("عند إنشاء العرض يجب أن تكون حالته للبيع أو للإيجار فقط.");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void Validate_NonPositiveArea_ReturnsError(string area)
    {
        _validator.TestValidate(Valid with { Area = decimal.Parse(area) })
            .ShouldHaveValidationErrorFor(c => c.Area)
            .WithErrorMessage("المساحة يجب أن تكون أكبر من صفر.");
    }

    [Fact]
    public void Validate_AreaAtMax_HasNoError()
    {
        _validator.TestValidate(Valid with { Area = Property.MaxArea }).ShouldNotHaveValidationErrorFor(c => c.Area);
    }

    [Fact]
    public void Validate_AreaOverMax_ReturnsError()
    {
        _validator.TestValidate(Valid with { Area = Property.MaxArea + 0.01m })
            .ShouldHaveValidationErrorFor(c => c.Area)
            .WithErrorMessage("المساحة أكبر من الحد المسموح.");
    }

    [Fact]
    public void Validate_AreaWithThreeDecimals_ReturnsError()
    {
        _validator.TestValidate(Valid with { Area = 150.125m })
            .ShouldHaveValidationErrorFor(c => c.Area)
            .WithErrorMessage("المساحة تقبل خانتين عشريتين كحد أقصى.");
    }

    [Fact]
    public void Validate_EmptyCity_ReturnsError()
    {
        _validator.TestValidate(Valid with { City = "" })
            .ShouldHaveValidationErrorFor(c => c.City)
            .WithErrorMessage("المدينة مطلوبة.");
    }

    [Fact]
    public void Validate_CityAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { City = Text(Property.MaxCityLength) }).ShouldNotHaveValidationErrorFor(c => c.City);
    }

    [Fact]
    public void Validate_CityOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { City = Text(Property.MaxCityLength + 1) }).ShouldHaveValidationErrorFor(c => c.City);
    }

    [Fact]
    public void Validate_RegionOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { Region = Text(Property.MaxRegionLength + 1) }).ShouldHaveValidationErrorFor(c => c.Region);
    }

    [Fact]
    public void Validate_RegionAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { Region = Text(Property.MaxRegionLength) }).ShouldNotHaveValidationErrorFor(c => c.Region);
    }

    [Fact]
    public void Validate_EmptyFullAddress_ReturnsError()
    {
        _validator.TestValidate(Valid with { FullAddress = "" })
            .ShouldHaveValidationErrorFor(c => c.FullAddress)
            .WithErrorMessage("العنوان الكامل مطلوب.");
    }

    [Fact]
    public void Validate_FullAddressAtMaxLength_HasNoError()
    {
        _validator.TestValidate(Valid with { FullAddress = Text(Property.MaxFullAddressLength) }).ShouldNotHaveValidationErrorFor(c => c.FullAddress);
    }

    [Fact]
    public void Validate_FullAddressOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { FullAddress = Text(Property.MaxFullAddressLength + 1) }).ShouldHaveValidationErrorFor(c => c.FullAddress);
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(0)]
    [InlineData(90)]
    public void Validate_LatitudeWithinRange_HasNoError(double latitude)
    {
        _validator.TestValidate(Valid with { Latitude = latitude }).ShouldNotHaveValidationErrorFor(c => c.Latitude);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_LatitudeOutOfRange_ReturnsError(double latitude)
    {
        _validator.TestValidate(Valid with { Latitude = latitude })
            .ShouldHaveValidationErrorFor(c => c.Latitude)
            .WithErrorMessage("يجب أن يكون خط العرض بين -90 و90.");
    }

    [Theory]
    [InlineData(-180)]
    [InlineData(0)]
    [InlineData(180)]
    public void Validate_LongitudeWithinRange_HasNoError(double longitude)
    {
        _validator.TestValidate(Valid with { Longitude = longitude }).ShouldNotHaveValidationErrorFor(c => c.Longitude);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_LongitudeOutOfRange_ReturnsError(double longitude)
    {
        _validator.TestValidate(Valid with { Longitude = longitude })
            .ShouldHaveValidationErrorFor(c => c.Longitude)
            .WithErrorMessage("يجب أن يكون خط الطول بين -180 و180.");
    }

    [Fact]
    public void Validate_UndefinedLandClassification_ReturnsError()
    {
        _validator.TestValidate(Valid with { LandClassification = (LandClassification)99 })
            .ShouldHaveValidationErrorFor(c => c.LandClassification)
            .WithErrorMessage("تصنيف الأرض غير صالح.");
    }

    [Fact]
    public void Validate_UndefinedLegalStatus_ReturnsError()
    {
        _validator.TestValidate(Valid with { LegalStatus = (LegalStatus)99 })
            .ShouldHaveValidationErrorFor(c => c.LegalStatus)
            .WithErrorMessage("الوضع القانوني للعقار غير صالح.");
    }
}
