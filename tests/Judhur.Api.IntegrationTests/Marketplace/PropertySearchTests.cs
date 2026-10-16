using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Domain.Properties.Enums;

using static Judhur.Api.IntegrationTests.Marketplace.Market;

namespace Judhur.Api.IntegrationTests.Marketplace;

[Collection(ApiCollection.Name)]
public sealed class PropertySearchTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Search_CityRepeated_ReturnsListingsInAnyOfTheCities()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var (cityA, cityB, cityC) = (NewCity(), NewCity(), NewCity());
        var inA = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { City = cityA });
        var inB = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { City = cityB });
        var inC = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { City = cityC });

        var page = await SearchAsync(_scenario.Anonymous(), $"{Query("city", cityA)}&{Query("city", cityB)}");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(new[] { inA, inB }.Order(), Ids(page).Order());
        Assert.DoesNotContain(inC, Ids(page));
    }

    [Fact]
    public async Task Search_SingleCity_MatchesIgnoringSurroundingSpaces()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var city = NewCity();
        var id = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { City = city });

        var page = await SearchAsync(_scenario.Anonymous(), Query("city", $"  {city}  "));

        Assert.Equal(new[] { id }, Ids(page));
    }

    [Fact]
    public async Task Search_PropertyTypeFilter_ReturnsOnlyMatchingTypes()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var apartment = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { PropertyType = PropertyType.Apartment });
        var land = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { PropertyType = PropertyType.Land });
        var house = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { PropertyType = PropertyType.House });

        var single = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&propertyType=Land");
        var several = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&propertyType=Land&propertyType=House");

        Assert.Equal(new[] { land }, Ids(single));
        Assert.Equal(new[] { land, house }.Order(), Ids(several).Order());
        Assert.DoesNotContain(apartment, Ids(several));
    }

    [Fact]
    public async Task Search_PropertyStatusFilter_ReturnsOnlyMatchingStatus()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var forSale = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { PropertyStatus = PropertyStatus.ForSale });
        var forRent = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { PropertyStatus = PropertyStatus.ForRent });

        var rent = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&propertyStatus=ForRent");
        var sale = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&propertyStatus=ForSale");

        Assert.Equal(new[] { forRent }, Ids(rent));
        Assert.Equal(new[] { forSale }, Ids(sale));
    }

    [Fact]
    public async Task Search_PriceRange_IsInclusiveOnBothEnds()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var cheap = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { Price = 100_000m });
        var middle = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { Price = 200_000m });
        var expensive = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { Price = 300_000m });

        var range = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&minPrice=150000&maxPrice=250000");
        var exact = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&minPrice=200000&maxPrice=200000");
        var atLeast = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&minPrice=200000");

        Assert.Equal(new[] { middle }, Ids(range));
        Assert.Equal(new[] { middle }, Ids(exact));
        Assert.Equal(new[] { middle, expensive }.Order(), Ids(atLeast).Order());
        Assert.DoesNotContain(cheap, Ids(atLeast));
    }

    [Fact]
    public async Task Search_SellerId_ReturnsOnlyThatSellersListings()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var admin = await _scenario.AdminAsync();
        var own = await CreateApprovedAsync(seller.Client, admin.Client);
        var foreign = await CreateApprovedAsync(other.Client, admin.Client);

        var page = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}");

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(new[] { own }, Ids(page));
        Assert.DoesNotContain(foreign, Ids(page));
    }

    [Fact]
    public async Task Search_SearchTerm_MatchesTitle()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var token = Guid.NewGuid().ToString("N");
        var id = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { Title = $"فيلا مميزة {token}" });
        await CreateApprovedAsync(seller.Client, admin.Client);

        var page = await SearchAsync(_scenario.Anonymous(), Query("searchTerm", token.ToUpperInvariant()));

        Assert.Equal(new[] { id }, Ids(page));
    }

    [Fact]
    public async Task Search_MixedListingStates_ReturnsOnlyApprovedAndActive()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var published = await CreateApprovedAsync(seller.Client, admin.Client);
        var pending = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var draft = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        var rejected = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        await RejectAsync(admin.Client, rejected);
        var deactivated = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{deactivated}/deactivate")).StatusCode);
        var deleted = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.NoContent, (await seller.Client.DeleteAsync($"{Routes.Properties}/{deleted}", Ct)).StatusCode);

        var page = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}");

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(new[] { published }, Ids(page));
        Assert.Equal(5, (await GetMyListAsync(seller.Client)).Count);
        Assert.All(new[] { pending, draft, rejected, deactivated, deleted }, id => Assert.DoesNotContain(id, Ids(page)));
    }

    [Fact]
    public async Task Search_AfterApproval_ShowsListingImmediately()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var before = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}");
        Assert.Empty(before.Items!);

        await Scenario.ApproveAsync(admin.Client, id);

        var after = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}");
        Assert.Equal(new[] { id }, Ids(after));
        Assert.Equal(1, after.TotalCount);
    }

    [Fact]
    public async Task Search_AfterDeactivation_HidesListingImmediately()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(new[] { id }, Ids(await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}")));

        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/deactivate")).StatusCode);

        Assert.Empty((await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}")).Items!);
    }

    [Fact]
    public async Task Search_SortByPrice_OrdersAscendingAndDescending()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var middle = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { Price = 200_000m });
        var cheap = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { Price = 100_000m });
        var expensive = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { Price = 300_000m });

        var ascending = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&sortColumn=price&sortDirection=asc");
        var descending = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&sortColumn=price&sortDirection=desc");

        Assert.Equal(new[] { cheap, middle, expensive }, Ids(ascending));
        Assert.Equal(new[] { expensive, middle, cheap }, Ids(descending));
    }

    [Fact]
    public async Task Search_SortByEqualPrices_PagesAreStableAndDoNotOverlap()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            ids.Add(await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { Price = 150_000m }));
        }
        var query = $"sellerId={seller.Id}&sortColumn=price&sortDirection=asc";

        var all = Ids(await SearchAsync(_scenario.Anonymous(), $"{query}&page=1&pageSize=3"));
        var paged = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var single = await SearchAsync(_scenario.Anonymous(), $"{query}&page={page}&pageSize=1");
            Assert.Equal(3, single.TotalCount);
            Assert.Equal(3, single.TotalPages);
            paged.AddRange(Ids(single));
        }

        Assert.Equal(ids.Order(), all.Order());
        Assert.Equal(all, paged);
    }

    [Fact]
    public async Task Search_PageBeyondLastResult_ReturnsEmptyPageWithTotals()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        await CreateApprovedAsync(seller.Client, admin.Client);

        var page = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}&page=2&pageSize=1");

        Assert.Empty(page.Items!);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
        Assert.Equal(2, page.PageNumber);
    }

    [Theory]
    [InlineData("page=0", "Pagination.PageInvalid")]
    [InlineData("page=-1", "Pagination.PageInvalid")]
    [InlineData("page=10001", "Pagination.PageInvalid")]
    [InlineData("pageSize=0", "Pagination.PageSizeInvalid")]
    [InlineData("pageSize=101", "Pagination.PageSizeInvalid")]
    public async Task Search_PaginationOutOfRange_Returns400(string query, string code)
    {
        var response = await _scenario.Anonymous().GetAsync($"{Routes.Properties}?{query}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, code);
    }

    [Fact]
    public async Task Search_MaxPage_Returns200()
    {
        var response = await _scenario.Anonymous().GetAsync($"{Routes.Properties}?page=10000&pageSize=100", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_MoreThanTwentyCities_Returns400()
    {
        var query = string.Join('&', Enumerable.Range(1, 21).Select(_ => Query("city", NewCity())));

        var response = await _scenario.Anonymous().GetAsync($"{Routes.Properties}?{query}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "PropertyErrors.TooManyCitiesInFilter");
    }
}
