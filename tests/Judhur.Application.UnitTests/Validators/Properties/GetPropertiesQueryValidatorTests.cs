using FluentValidation.TestHelper;

using Judhur.Application.Features.Properties.Queries.GetProperties;
using Judhur.Domain.Properties;

namespace Judhur.Application.UnitTests.Validators.Properties;

public sealed class GetPropertiesQueryValidatorTests
{
    private static readonly GetPropertiesQuery Valid = new(
        Page: 1,
        PageSize: 20,
        SearchTerm: null,
        MinPrice: null,
        MaxPrice: null,
        Cities: [],
        LandClassifications: [],
        LegalStatuses: [],
        PaymentTypes: [],
        PropertyStatuses: [],
        PropertyTypes: [],
        SellerId: null);

    private readonly GetPropertiesQueryValidator _validator = new();

    [Fact]
    public void Validate_NoFilters_HasNoErrors()
    {
        _validator.TestValidate(Valid).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_PriceRange_HasNoErrors()
    {
        _validator.TestValidate(Valid with { MinPrice = 10_000, MaxPrice = 250_000 }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EqualMinAndMax_HasNoErrors()
    {
        _validator.TestValidate(Valid with { MinPrice = 50_000, MaxPrice = 50_000 }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NegativeMinPrice_ReturnsError()
    {
        _validator.TestValidate(Valid with { MinPrice = -1 })
            .ShouldHaveValidationErrorFor(q => q.MinPrice)
            .WithErrorMessage("الحد الأدنى للسعر غير صالح.");
    }

    [Fact]
    public void Validate_MaxPriceAboveDomainLimit_ReturnsError()
    {
        _validator.TestValidate(Valid with { MaxPrice = Property.MaxPrice + 1 })
            .ShouldHaveValidationErrorFor(q => q.MaxPrice)
            .WithErrorMessage("الحد الأعلى للسعر غير صالح.");
    }

    [Fact]
    public void Validate_MaxBelowMin_ReturnsError()
    {
        _validator.TestValidate(Valid with { MinPrice = 200_000, MaxPrice = 100_000 })
            .ShouldHaveValidationErrorFor(q => q.MaxPrice)
            .WithErrorMessage("الحد الأعلى للسعر يجب أن يكون أكبر من الحد الأدنى أو مساويًا له.");
    }

    [Fact]
    public void Validate_SearchTermAtLimit_HasNoErrors()
    {
        _validator.TestValidate(Valid with { SearchTerm = new string('ش', GetPropertiesQueryValidator.MaxSearchTermLength) })
            .ShouldNotHaveValidationErrorFor(q => q.SearchTerm);
    }

    [Fact]
    public void Validate_SearchTermTooLong_ReturnsError()
    {
        _validator.TestValidate(Valid with { SearchTerm = new string('ش', GetPropertiesQueryValidator.MaxSearchTermLength + 1) })
            .ShouldHaveValidationErrorFor(q => q.SearchTerm);
    }
}
