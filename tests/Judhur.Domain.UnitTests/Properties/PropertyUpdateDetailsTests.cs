using System.Globalization;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Tests.Common.Factories;

namespace Judhur.Domain.UnitTests.Properties;

public sealed class PropertyUpdateDetailsTests
{
    private static readonly PropertyDetails NewDetails = new()
    {
        Title = "أرض زراعية في نابلس",
        Description = "أرض مزروعة بالزيتون",
        Price = 99_999.99m,
        PaymentType = PaymentType.Negotiable,
        PropertyType = PropertyType.Land,
        Area = 1_000.25m,
        City = "نابلس",
        Region = null,
        FullAddress = "نابلس، بيت إيبا",
        Latitude = 32.2211,
        Longitude = 35.2544,
        LandClassification = LandClassification.C,
        LegalStatus = LegalStatus.Taswiye
    };

    private static decimal Dec(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [Fact]
    public void UpdateDetails_ValidDetails_ReturnsSuccess()
    {
        var property = PropertyFactory.Create();

        var result = PropertyFactory.UpdateDetails(property, NewDetails);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void UpdateDetails_ValidDetails_ReplacesAllDetails()
    {
        var property = PropertyFactory.Create();

        PropertyFactory.UpdateDetails(property, NewDetails);

        Assert.Equal(NewDetails.Title, property.Title);
        Assert.Equal(NewDetails.Description, property.Description);
        Assert.Equal(NewDetails.Price, property.Price);
        Assert.Equal(NewDetails.PaymentType, property.PaymentType);
        Assert.Equal(NewDetails.PropertyType, property.PropertyType);
        Assert.Equal(NewDetails.Area, property.Area);
        Assert.Equal(NewDetails.City, property.City);
        Assert.Equal(NewDetails.Region, property.Region);
        Assert.Equal(NewDetails.FullAddress, property.FullAddress);
        Assert.Equal(NewDetails.Latitude, property.Latitude);
        Assert.Equal(NewDetails.Longitude, property.Longitude);
        Assert.Equal(NewDetails.LandClassification, property.LandClassification);
        Assert.Equal(NewDetails.LegalStatus, property.LegalStatus);
    }

    [Fact]
    public void UpdateDetails_ValidDetails_KeepsMarketStatusSellerAndActiveState()
    {
        var sellerId = Guid.NewGuid();
        var property = PropertyFactory.Create(PropertyDetails.Valid with { PropertyStatus = PropertyStatus.ForRent }, sellerId);
        property.Deactivate();

        PropertyFactory.UpdateDetails(property, NewDetails);

        Assert.Equal(PropertyStatus.ForRent, property.PropertyStatus);
        Assert.Equal(sellerId, property.SellerId);
        Assert.False(property.IsActive);
    }

    [Fact]
    public void UpdateDetails_InvalidDetails_DoesNotChangeProperty()
    {
        var property = PropertyFactory.Create();
        var originalTitle = property.Title;
        var originalPrice = property.Price;

        PropertyFactory.UpdateDetails(property, NewDetails with { City = "" });

        Assert.Equal(originalTitle, property.Title);
        Assert.Equal(originalPrice, property.Price);
    }

    [Fact]
    public void UpdateDetails_InvalidDetailsOnApprovedProperty_KeepsApproval()
    {
        var property = PropertyFactory.CreateApproved();

        PropertyFactory.UpdateDetails(property, NewDetails with { Title = "" });

        Assert.Equal(ModerationStatus.Approved, property.ModerationStatus);
    }

    [Fact]
    public void UpdateDetails_BlankTitle_ReturnsTitleRequired()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Title = " " });

        Assert.Equal(PropertyErrors.TitleRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_TitleOverLimit_ReturnsTitleTooLong()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Title = new string('x', Property.MaxTitleLength + 1) });

        Assert.Equal(PropertyErrors.TitleTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_DescriptionOverLimit_ReturnsDescriptionTooLong()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Description = new string('x', Property.MaxDescriptionLength + 1) });

        Assert.Equal(PropertyErrors.DescriptionTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_ZeroPrice_ReturnsPriceInvalid()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Price = 0m });

        Assert.Equal(PropertyErrors.PriceInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_PriceAboveMax_ReturnsPriceTooHigh()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Price = Property.MaxPrice + 0.01m });

        Assert.Equal(PropertyErrors.PriceTooHigh.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_PriceAtMax_ReturnsSuccess()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Price = Property.MaxPrice });

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("10.001")]
    [InlineData("0.009")]
    public void UpdateDetails_PriceWithMoreThanTwoDecimals_ReturnsPriceTooPrecise(string price)
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Price = Dec(price) });

        Assert.Equal(PropertyErrors.PriceTooPrecise.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_UndefinedPaymentType_ReturnsPaymentInvalid()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { PaymentType = (PaymentType)42 });

        Assert.Equal(PropertyErrors.PaymentInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_UndefinedPropertyType_ReturnsPropertyTypeInvalid()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { PropertyType = (PropertyType)42 });

        Assert.Equal(PropertyErrors.PropertyTypeInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_NegativeArea_ReturnsAreaInvalid()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Area = -1m });

        Assert.Equal(PropertyErrors.AreaInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_AreaAboveMax_ReturnsAreaTooHigh()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Area = Property.MaxArea + 1m });

        Assert.Equal(PropertyErrors.AreaTooHigh.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_AreaAtMax_ReturnsSuccess()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Area = Property.MaxArea });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void UpdateDetails_AreaWithMoreThanTwoDecimals_ReturnsAreaTooPrecise()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Area = Dec("120.125") });

        Assert.Equal(PropertyErrors.AreaTooPrecise.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_BlankCity_ReturnsCityRequired()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { City = "" });

        Assert.Equal(PropertyErrors.CityRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_CityOverLimit_ReturnsCityTooLong()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { City = new string('x', Property.MaxCityLength + 1) });

        Assert.Equal(PropertyErrors.CityTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_RegionOverLimit_ReturnsRegionTooLong()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Region = new string('x', Property.MaxRegionLength + 1) });

        Assert.Equal(PropertyErrors.RegionTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_BlankFullAddress_ReturnsFullAddressRequired()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { FullAddress = "\t" });

        Assert.Equal(PropertyErrors.FullAddressRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_FullAddressOverLimit_ReturnsFullAddressTooLong()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { FullAddress = new string('x', Property.MaxFullAddressLength + 1) });

        Assert.Equal(PropertyErrors.FullAddressTooLong.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(-91.0)]
    [InlineData(91.0)]
    public void UpdateDetails_LatitudeOutOfRange_ReturnsLatitudeInvalid(double latitude)
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Latitude = latitude });

        Assert.Equal(PropertyErrors.LatitudeInvalid.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(-181.0)]
    [InlineData(181.0)]
    public void UpdateDetails_LongitudeOutOfRange_ReturnsLongitudeInvalid(double longitude)
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { Longitude = longitude });

        Assert.Equal(PropertyErrors.LongitudeInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_UndefinedLandClassification_ReturnsLandClassificationInvalid()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { LandClassification = (LandClassification)42 });

        Assert.Equal(PropertyErrors.LandClassificationInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDetails_UndefinedLegalStatus_ReturnsLegalStatusInvalid()
    {
        var result = PropertyFactory.UpdateDetails(PropertyFactory.Create(), NewDetails with { LegalStatus = (LegalStatus)42 });

        Assert.Equal(PropertyErrors.LegalStatusInvalid.Code, result.TopError.Code);
    }
}
