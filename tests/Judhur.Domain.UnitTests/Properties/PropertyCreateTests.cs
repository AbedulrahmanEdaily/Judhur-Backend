using System.Globalization;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyCreateTests
{
    private static decimal Dec(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [Fact]
    public void Create_ValidDetails_ReturnsSuccess()
    {
        var result = PropertyFactory.CreateResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ValidDetails_CopiesAllDetails()
    {
        var details = PropertyDetails.Valid with
        {
            PaymentType = PaymentType.Installments,
            PropertyType = PropertyType.House,
            PropertyStatus = PropertyStatus.ForRent,
            LandClassification = LandClassification.B,
            LegalStatus = LegalStatus.Maliye
        };
        var sellerId = Guid.NewGuid();

        var property = PropertyFactory.Create(details, sellerId);

        Assert.Equal(details.Title, property.Title);
        Assert.Equal(details.Description, property.Description);
        Assert.Equal(details.Price, property.Price);
        Assert.Equal(details.PaymentType, property.PaymentType);
        Assert.Equal(details.PropertyType, property.PropertyType);
        Assert.Equal(details.PropertyStatus, property.PropertyStatus);
        Assert.Equal(details.Area, property.Area);
        Assert.Equal(details.City, property.City);
        Assert.Equal(details.Region, property.Region);
        Assert.Equal(details.FullAddress, property.FullAddress);
        Assert.Equal(details.Latitude, property.Latitude);
        Assert.Equal(details.Longitude, property.Longitude);
        Assert.Equal(details.LandClassification, property.LandClassification);
        Assert.Equal(details.LegalStatus, property.LegalStatus);
        Assert.Equal(sellerId, property.SellerId);
    }

    [Fact]
    public void Create_ValidDetails_StartsPendingActiveAndNotDeleted()
    {
        var property = PropertyFactory.Create();

        Assert.Equal(ModerationStatus.Pending, property.ModerationStatus);
        Assert.True(property.IsActive);
        Assert.False(property.IsDeleted);
        Assert.Null(property.DeletedAtUtc);
    }

    [Fact]
    public void Create_ValidDetails_HasNoReviewDataImagesOrDocument()
    {
        var property = PropertyFactory.Create();

        Assert.Null(property.RejectionReason);
        Assert.Null(property.ReviewedBy);
        Assert.Null(property.ReviewedAtUtc);
        Assert.Null(property.OwnershipDocumentPublicId);
        Assert.Empty(property.PropertyImages);
        Assert.Null(property.MainImage);
    }

    [Fact]
    public void Create_GivenId_UsesThatId()
    {
        var id = Guid.NewGuid();

        var property = PropertyFactory.Create(id: id);

        Assert.Equal(id, property.Id);
    }

    [Fact]
    public void Create_EmptyId_GeneratesNewId()
    {
        var property = PropertyFactory.Create(id: Guid.Empty);

        Assert.NotEqual(Guid.Empty, property.Id);
    }

    [Theory]
    [InlineData(PropertyStatus.ForSale)]
    [InlineData(PropertyStatus.ForRent)]
    public void Create_InitialMarketStatus_ReturnsSuccess(PropertyStatus status)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { PropertyStatus = status });

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(PropertyStatus.Sold)]
    [InlineData(PropertyStatus.Rented)]
    [InlineData((PropertyStatus)99)]
    public void Create_NonInitialMarketStatus_ReturnsInitialPropertyStatusInvalid(PropertyStatus status)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { PropertyStatus = status });

        Assert.Equal(PropertyErrors.InitialPropertyStatusInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_EmptySeller_ReturnsSellerRequired()
    {
        var result = PropertyFactory.CreateResult(sellerId: Guid.Empty);

        Assert.Equal(PropertyErrors.SellerRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_InvalidDetailsAndInvalidStatus_ReturnsDetailsErrorFirst()
    {
        var result = PropertyFactory.CreateResult(
            PropertyDetails.Valid with { Title = "", PropertyStatus = PropertyStatus.Sold },
            Guid.Empty);

        Assert.Equal(PropertyErrors.TitleRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_InvalidDetails_ReturnsSingleError()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Title = "" });

        Assert.Single(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Create_BlankTitle_ReturnsTitleRequired(string title)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Title = title });

        Assert.Equal(PropertyErrors.TitleRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_NullTitle_ReturnsTitleRequired()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Title = null! });

        Assert.Equal(PropertyErrors.TitleRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(Property.MaxTitleLength)]
    public void Create_TitleWithinLimit_ReturnsSuccess(int length)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Title = new string('ع', length) });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_TitleOverLimit_ReturnsTitleTooLong()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Title = new string('ع', Property.MaxTitleLength + 1) });

        Assert.Equal(PropertyErrors.TitleTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_NullDescription_ReturnsSuccess()
    {
        var property = PropertyFactory.Create(PropertyDetails.Valid with { Description = null });

        Assert.Null(property.Description);
    }

    [Fact]
    public void Create_DescriptionAtLimit_ReturnsSuccess()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Description = new string('ع', Property.MaxDescriptionLength) });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_DescriptionOverLimit_ReturnsDescriptionTooLong()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Description = new string('ع', Property.MaxDescriptionLength + 1) });

        Assert.Equal(PropertyErrors.DescriptionTooLong.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-1000")]
    public void Create_NonPositivePrice_ReturnsPriceInvalid(string price)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Price = Dec(price) });

        Assert.Equal(PropertyErrors.PriceInvalid.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("1")]
    [InlineData("1.5")]
    [InlineData("1.50")]
    [InlineData("999.99")]
    [InlineData("1000000000000")]
    [InlineData("1000000000000.00")]
    public void Create_ValidPrice_ReturnsSuccess(string price)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Price = Dec(price) });

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("1000000000000.01")]
    [InlineData("1000000000001")]
    [InlineData("79228162514264337593543950335")]
    public void Create_PriceAboveMax_ReturnsPriceTooHigh(string price)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Price = Dec(price) });

        Assert.Equal(PropertyErrors.PriceTooHigh.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1.005")]
    [InlineData("100.125")]
    [InlineData("250000.0001")]
    public void Create_PriceWithMoreThanTwoDecimals_ReturnsPriceTooPrecise(string price)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Price = Dec(price) });

        Assert.Equal(PropertyErrors.PriceTooPrecise.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_PriceWithTrailingZeroDecimals_ReturnsSuccess()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Price = Dec("1.2300") });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_UndefinedPaymentType_ReturnsPaymentInvalid()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { PaymentType = (PaymentType)99 });

        Assert.Equal(PropertyErrors.PaymentInvalid.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(PaymentType.Cash)]
    [InlineData(PaymentType.Installments)]
    [InlineData(PaymentType.DownPaymentAndInstallments)]
    [InlineData(PaymentType.Negotiable)]
    public void Create_DefinedPaymentType_ReturnsSuccess(PaymentType paymentType)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { PaymentType = paymentType });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_UndefinedPropertyType_ReturnsPropertyTypeInvalid()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { PropertyType = (PropertyType)(-1) });

        Assert.Equal(PropertyErrors.PropertyTypeInvalid.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(PropertyType.Apartment)]
    [InlineData(PropertyType.House)]
    [InlineData(PropertyType.Land)]
    [InlineData(PropertyType.Office)]
    [InlineData(PropertyType.Storage)]
    [InlineData(PropertyType.Building)]
    public void Create_DefinedPropertyType_ReturnsSuccess(PropertyType propertyType)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { PropertyType = propertyType });

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-50")]
    public void Create_NonPositiveArea_ReturnsAreaInvalid(string area)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Area = Dec(area) });

        Assert.Equal(PropertyErrors.AreaInvalid.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("75.5")]
    [InlineData("10000000")]
    [InlineData("10000000.00")]
    public void Create_ValidArea_ReturnsSuccess(string area)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Area = Dec(area) });

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("10000000.01")]
    [InlineData("10000001")]
    public void Create_AreaAboveMax_ReturnsAreaTooHigh(string area)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Area = Dec(area) });

        Assert.Equal(PropertyErrors.AreaTooHigh.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("150.555")]
    public void Create_AreaWithMoreThanTwoDecimals_ReturnsAreaTooPrecise(string area)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Area = Dec(area) });

        Assert.Equal(PropertyErrors.AreaTooPrecise.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankCity_ReturnsCityRequired(string city)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { City = city });

        Assert.Equal(PropertyErrors.CityRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_CityAtLimit_ReturnsSuccess()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { City = new string('ع', Property.MaxCityLength) });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_CityOverLimit_ReturnsCityTooLong()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { City = new string('ع', Property.MaxCityLength + 1) });

        Assert.Equal(PropertyErrors.CityTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_NullRegion_ReturnsSuccess()
    {
        var property = PropertyFactory.Create(PropertyDetails.Valid with { Region = null });

        Assert.Null(property.Region);
    }

    [Fact]
    public void Create_RegionAtLimit_ReturnsSuccess()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Region = new string('ع', Property.MaxRegionLength) });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_RegionOverLimit_ReturnsRegionTooLong()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Region = new string('ع', Property.MaxRegionLength + 1) });

        Assert.Equal(PropertyErrors.RegionTooLong.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_BlankFullAddress_ReturnsFullAddressRequired(string fullAddress)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { FullAddress = fullAddress });

        Assert.Equal(PropertyErrors.FullAddressRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_FullAddressAtLimit_ReturnsSuccess()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { FullAddress = new string('ع', Property.MaxFullAddressLength) });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_FullAddressOverLimit_ReturnsFullAddressTooLong()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { FullAddress = new string('ع', Property.MaxFullAddressLength + 1) });

        Assert.Equal(PropertyErrors.FullAddressTooLong.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(-90.0)]
    [InlineData(0.0)]
    [InlineData(90.0)]
    public void Create_LatitudeWithinRange_ReturnsSuccess(double latitude)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Latitude = latitude });

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_LatitudeOutOfRange_ReturnsLatitudeInvalid(double latitude)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Latitude = latitude });

        Assert.Equal(PropertyErrors.LatitudeInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_NaNLatitude_ReturnsLatitudeInvalid()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Latitude = double.NaN });

        Assert.Equal(PropertyErrors.LatitudeInvalid.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(-180.0)]
    [InlineData(0.0)]
    [InlineData(180.0)]
    public void Create_LongitudeWithinRange_ReturnsSuccess(double longitude)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Longitude = longitude });

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_LongitudeOutOfRange_ReturnsLongitudeInvalid(double longitude)
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Longitude = longitude });

        Assert.Equal(PropertyErrors.LongitudeInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_NaNLongitude_ReturnsLongitudeInvalid()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Longitude = double.NaN });

        Assert.Equal(PropertyErrors.LongitudeInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_UndefinedLandClassification_ReturnsLandClassificationInvalid()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { LandClassification = (LandClassification)7 });

        Assert.Equal(PropertyErrors.LandClassificationInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_UndefinedLegalStatus_ReturnsLegalStatusInvalid()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { LegalStatus = (LegalStatus)7 });

        Assert.Equal(PropertyErrors.LegalStatusInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_SeveralInvalidFields_ReturnsFirstFailingRuleInOrder()
    {
        var result = PropertyFactory.CreateResult(PropertyDetails.Valid with { Price = 0m, Area = 0m, City = "" });

        Assert.Equal(PropertyErrors.PriceInvalid.Code, result.TopError.Code);
    }
}
