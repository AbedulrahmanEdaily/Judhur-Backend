using FluentValidation.TestHelper;

using Judhur.Application.Features.Properties.Commands.UpdatePropertyDetails;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Properties;

public sealed class UpdatePropertyDetailsCommandValidatorTests
{
    private static readonly UpdatePropertyDetailsCommand Valid = new(
        Guid.NewGuid(),
        "بيت مستقل في نابلس",
        180_000m,
        PaymentType.Installments,
        PropertyType.House,
        220m,
        "نابلس",
        null,
        "نابلس، رفيديا",
        32.2211,
        35.2544,
        LandClassification.B,
        LegalStatus.Maliye);

    private readonly UpdatePropertyDetailsCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyPropertyId_ReturnsError()
    {
        _validator.TestValidate(Valid with { PropertyId = Guid.Empty })
            .ShouldHaveValidationErrorFor(c => c.PropertyId)
            .WithErrorMessage("معرّف العقار مطلوب.");
    }

    [Fact]
    public void Validate_EmptyTitle_ReturnsError()
    {
        _validator.TestValidate(Valid with { Title = "" }).ShouldHaveValidationErrorFor(c => c.Title);
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
    public void Validate_ZeroPrice_ReturnsError()
    {
        _validator.TestValidate(Valid with { Price = 0m }).ShouldHaveValidationErrorFor(c => c.Price);
    }

    [Fact]
    public void Validate_PriceAtMax_HasNoError()
    {
        _validator.TestValidate(Valid with { Price = Property.MaxPrice }).ShouldNotHaveValidationErrorFor(c => c.Price);
    }

    [Fact]
    public void Validate_PriceOverMax_ReturnsError()
    {
        _validator.TestValidate(Valid with { Price = Property.MaxPrice + 1m }).ShouldHaveValidationErrorFor(c => c.Price);
    }

    [Fact]
    public void Validate_PriceWithThreeDecimals_ReturnsError()
    {
        _validator.TestValidate(Valid with { Price = 10.001m }).ShouldHaveValidationErrorFor(c => c.Price);
    }

    [Fact]
    public void Validate_UndefinedPaymentType_ReturnsError()
    {
        _validator.TestValidate(Valid with { PaymentType = (PaymentType)(-1) }).ShouldHaveValidationErrorFor(c => c.PaymentType);
    }

    [Fact]
    public void Validate_UndefinedPropertyType_ReturnsError()
    {
        _validator.TestValidate(Valid with { PropertyType = (PropertyType)42 }).ShouldHaveValidationErrorFor(c => c.PropertyType);
    }

    [Fact]
    public void Validate_ZeroArea_ReturnsError()
    {
        _validator.TestValidate(Valid with { Area = 0m }).ShouldHaveValidationErrorFor(c => c.Area);
    }

    [Fact]
    public void Validate_AreaOverMax_ReturnsError()
    {
        _validator.TestValidate(Valid with { Area = Property.MaxArea + 1m }).ShouldHaveValidationErrorFor(c => c.Area);
    }

    [Fact]
    public void Validate_AreaWithThreeDecimals_ReturnsError()
    {
        _validator.TestValidate(Valid with { Area = 1.001m }).ShouldHaveValidationErrorFor(c => c.Area);
    }

    [Fact]
    public void Validate_EmptyCity_ReturnsError()
    {
        _validator.TestValidate(Valid with { City = "" }).ShouldHaveValidationErrorFor(c => c.City);
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
    public void Validate_EmptyFullAddress_ReturnsError()
    {
        _validator.TestValidate(Valid with { FullAddress = "" }).ShouldHaveValidationErrorFor(c => c.FullAddress);
    }

    [Fact]
    public void Validate_FullAddressOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(Valid with { FullAddress = Text(Property.MaxFullAddressLength + 1) }).ShouldHaveValidationErrorFor(c => c.FullAddress);
    }

    [Theory]
    [InlineData(-90.5)]
    [InlineData(90.5)]
    [InlineData(double.NaN)]
    public void Validate_LatitudeOutOfRange_ReturnsError(double latitude)
    {
        _validator.TestValidate(Valid with { Latitude = latitude }).ShouldHaveValidationErrorFor(c => c.Latitude);
    }

    [Theory]
    [InlineData(-180.5)]
    [InlineData(180.5)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_LongitudeOutOfRange_ReturnsError(double longitude)
    {
        _validator.TestValidate(Valid with { Longitude = longitude }).ShouldHaveValidationErrorFor(c => c.Longitude);
    }

    [Fact]
    public void Validate_UndefinedLandClassification_ReturnsError()
    {
        _validator.TestValidate(Valid with { LandClassification = (LandClassification)7 }).ShouldHaveValidationErrorFor(c => c.LandClassification);
    }

    [Fact]
    public void Validate_UndefinedLegalStatus_ReturnsError()
    {
        _validator.TestValidate(Valid with { LegalStatus = (LegalStatus)7 }).ShouldHaveValidationErrorFor(c => c.LegalStatus);
    }
}
