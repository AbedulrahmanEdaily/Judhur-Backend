using Judhur.Application.Features.Properties.Queries.GetProperties;
using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.UnitTests.Caching;

public sealed class GetPropertiesQueryCacheKeyTests
{
    private static readonly GetPropertiesQuery Default = new(
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

    [Fact]
    public void CacheKey_HasPropertiesSearchPrefix()
    {
        Assert.StartsWith("properties:search:", Default.CacheKey);
    }

    [Fact]
    public void CacheKey_SameQuery_IsStable()
    {
        Assert.Equal(Default.CacheKey, (Default with { }).CacheKey);
    }

    [Fact]
    public void CacheKey_DifferentPage_Differs()
    {
        Assert.NotEqual(Default.CacheKey, (Default with { Page = 2 }).CacheKey);
    }

    [Fact]
    public void CacheKey_DifferentPageSize_Differs()
    {
        Assert.NotEqual(Default.CacheKey, (Default with { PageSize = 10 }).CacheKey);
    }

    [Fact]
    public void CacheKey_DifferentPriceRange_Differs()
    {
        Assert.NotEqual(Default.CacheKey, (Default with { MinPrice = 1000m }).CacheKey);
        Assert.NotEqual(Default.CacheKey, (Default with { MaxPrice = 1000m }).CacheKey);
    }

    [Fact]
    public void CacheKey_DifferentSeller_Differs()
    {
        Assert.NotEqual(Default.CacheKey, (Default with { SellerId = Guid.NewGuid() }).CacheKey);
    }

    [Fact]
    public void CacheKey_CitiesInDifferentOrderAndCase_AreEqual()
    {
        var first = Default with { Cities = ["Nablus", "Jenin"] };
        var second = Default with { Cities = ["jenin", "NABLUS", "nablus"] };

        Assert.Equal(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void CacheKey_EnumFiltersInDifferentOrder_AreEqual()
    {
        var first = Default with
        {
            PropertyTypes = [PropertyType.House, PropertyType.Land],
            PaymentTypes = [PaymentType.Cash, PaymentType.Installments],
        };
        var second = Default with
        {
            PropertyTypes = [PropertyType.Land, PropertyType.House, PropertyType.Land],
            PaymentTypes = [PaymentType.Installments, PaymentType.Cash],
        };

        Assert.Equal(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void CacheKey_DifferentEnumFilter_Differs()
    {
        Assert.NotEqual(
            (Default with { LegalStatuses = [LegalStatus.Tabo] }).CacheKey,
            (Default with { LegalStatuses = [LegalStatus.Maliye] }).CacheKey);
    }

    [Fact]
    public void CacheKey_SortColumnIgnoresCase()
    {
        Assert.Equal(
            (Default with { SortColumn = "price" }).CacheKey,
            (Default with { SortColumn = "PRICE" }).CacheKey);
    }

    [Fact]
    public void CacheKey_SortDirectionIgnoresCase()
    {
        Assert.Equal(
            (Default with { SortColumn = "price", SortDirection = "DESC" }).CacheKey,
            (Default with { SortColumn = "price", SortDirection = "desc" }).CacheKey);
    }

    [Fact]
    public void CacheKey_DifferentSortDirection_Differs()
    {
        Assert.NotEqual(
            (Default with { SortColumn = "price", SortDirection = "asc" }).CacheKey,
            (Default with { SortColumn = "price", SortDirection = "desc" }).CacheKey);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public void CacheKey_UnknownSortColumn_MatchesNewestFirst(string direction)
    {
        Assert.Equal(
            Default.CacheKey,
            (Default with { SortColumn = "bogus", SortDirection = direction }).CacheKey);
    }

    [Fact]
    public void CacheKey_SearchTermIsNotPartOfKey()
    {
        Assert.Equal(Default.CacheKey, (Default with { SearchTerm = "شقة" }).CacheKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsCacheable_NoSearchTerm_ReturnsTrue(string? searchTerm)
    {
        Assert.True((Default with { SearchTerm = searchTerm }).IsCacheable);
    }

    [Fact]
    public void IsCacheable_SearchTerm_ReturnsFalse()
    {
        Assert.False((Default with { SearchTerm = "شقة" }).IsCacheable);
    }

    [Fact]
    public void TagsAndExpiration()
    {
        Assert.Equal(["properties"], Default.Tags);
        Assert.Equal(TimeSpan.FromMinutes(10), Default.Expiration);
    }

    [Fact]
    public void IsCacheable_LastCachedPage_ReturnsTrue()
    {
        Assert.True((Default with { Page = GetPropertiesQuery.MaxCachedPage }).IsCacheable);
    }

    [Fact]
    public void IsCacheable_DeepPage_ReturnsFalse()
    {
        Assert.False((Default with { Page = GetPropertiesQuery.MaxCachedPage + 1 }).IsCacheable);
    }
}
