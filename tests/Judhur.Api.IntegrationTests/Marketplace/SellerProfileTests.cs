using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Features.Sellers.Dto;

using static Judhur.Api.IntegrationTests.Marketplace.Market;

namespace Judhur.Api.IntegrationTests.Marketplace;

[Collection(ApiCollection.Name)]
public sealed class SellerProfileTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    private static string SellerUrl(Guid sellerId) => $"{Routes.Sellers}/{sellerId}";

    private async Task<SellerProfileDto> GetProfileAsync(Guid sellerId)
    {
        var response = await _scenario.Anonymous().GetAsync(SellerUrl(sellerId), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<SellerProfileDto>();
    }

    [Fact]
    public async Task Get_NewSeller_ReturnsPublicProfileWithoutContactDetails()
    {
        var fullName = $"بائع {Guid.NewGuid():N}";
        var seller = await _scenario.NewUserAsync("seller", fullName);

        var response = await _scenario.Anonymous().GetAsync(SellerUrl(seller.Id), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal(seller.Id, json.GetProperty("id").GetGuid());
        Assert.Equal(fullName, json.GetProperty("fullName").GetString());
        Assert.Equal("نابلس", json.GetProperty("city").GetString());
        Assert.Equal(0, json.GetProperty("activeListingsCount").GetInt32());
        Assert.Equal(0, json.GetProperty("reviewsCount").GetInt32());
        Assert.True(IsNullOrMissing(json, "averageRating"));
        Assert.False(json.TryGetProperty("email", out _));
        Assert.False(json.TryGetProperty("phoneNumber", out _));
    }

    [Fact]
    public async Task Get_AfterApproval_CountsOnlyPublishedListings()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        Assert.Equal(0, (await GetProfileAsync(seller.Id)).ActiveListingsCount);

        await Scenario.ApproveAsync(admin.Client, id);

        Assert.Equal(1, (await GetProfileAsync(seller.Id)).ActiveListingsCount);
    }

    [Fact]
    public async Task Get_AfterDeactivation_DropsListingFromCount()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(1, (await GetProfileAsync(seller.Id)).ActiveListingsCount);

        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/deactivate")).StatusCode);

        Assert.Equal(0, (await GetProfileAsync(seller.Id)).ActiveListingsCount);
    }

    [Fact]
    public async Task Listings_SellerIdFilter_MatchesProfileCount()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var admin = await _scenario.AdminAsync();
        var first = await CreateApprovedAsync(seller.Client, admin.Client);
        var second = await CreateApprovedAsync(seller.Client, admin.Client);
        await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        await CreateApprovedAsync(other.Client, admin.Client);

        var listings = await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}");
        var profile = await GetProfileAsync(seller.Id);

        Assert.Equal(new[] { second, first }, Ids(listings));
        Assert.Equal(profile.ActiveListingsCount, listings.TotalCount);
    }

    [Fact]
    public async Task Get_BannedSeller_Returns404AndHidesListings()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(1, (await GetProfileAsync(seller.Id)).ActiveListingsCount);
        Assert.Equal(new[] { id }, Ids(await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}")));
        Assert.Equal(HttpStatusCode.OK, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);

        await BanAsync(admin.Client, seller.Id);

        await AssertErrorAsync(await _scenario.Anonymous().GetAsync(SellerUrl(seller.Id), Ct), HttpStatusCode.NotFound, "Seller.NotFound");
        Assert.Empty((await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}")).Items!);
        Assert.Equal(HttpStatusCode.NotFound, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
    }

    [Fact]
    public async Task Get_Administrator_Returns404()
    {
        var admin = await _scenario.AdminAsync();

        var response = await _scenario.Anonymous().GetAsync(SellerUrl(admin.Id), Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Seller.NotFound");
    }

    [Fact]
    public async Task Get_UnknownSeller_Returns404()
    {
        var response = await _scenario.Anonymous().GetAsync(SellerUrl(Guid.NewGuid()), Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Seller.NotFound");
    }
}
