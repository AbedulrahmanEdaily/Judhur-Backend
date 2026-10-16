using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Dto;

using static Judhur.Api.IntegrationTests.Marketplace.Market;

namespace Judhur.Api.IntegrationTests.Marketplace;

[Collection(ApiCollection.Name)]
public sealed class FavoriteTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    private static string FavoriteUrl(Guid propertyId) => $"{Routes.Favorites}/{propertyId}";

    private static async Task<List<Guid>> FavoriteIdsAsync(HttpClient client)
    {
        var response = await client.GetAsync($"{Routes.Favorites}/ids", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<List<Guid>>();
    }

    private static async Task<PaginatedList<PropertySummaryDto>> FavoritesAsync(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"{Routes.Favorites}?{query}", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<PaginatedList<PropertySummaryDto>>();
    }

    [Fact]
    public async Task Add_ApprovedListing_AppearsInIdsAndList()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var body = new ListingBody();
        var id = await CreateApprovedAsync(seller.Client, admin.Client, body);

        var response = await PostEmptyAsync(buyer.Client, FavoriteUrl(id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(new[] { id }, await FavoriteIdsAsync(buyer.Client));
        var list = await FavoritesAsync(buyer.Client);
        var item = Assert.Single(list.Items!);
        Assert.Equal(id, item.Id);
        Assert.Equal(body.Title, item.Title);
        Assert.StartsWith(FilesHost, item.MainImageUrl);
        Assert.Equal(1, list.TotalCount);
        Assert.Empty(await FavoriteIdsAsync(seller.Client));
    }

    [Fact]
    public async Task Add_Twice_Returns409AlreadyFavorite()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(buyer.Client, FavoriteUrl(id))).StatusCode);

        var response = await PostEmptyAsync(buyer.Client, FavoriteUrl(id));

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "FavoriteErrors.AlreadyFavorite");
        Assert.Single(await FavoriteIdsAsync(buyer.Client));
    }

    [Fact]
    public async Task Add_PendingListing_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());

        var response = await PostEmptyAsync(buyer.Client, FavoriteUrl(id));

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
        Assert.Empty(await FavoriteIdsAsync(buyer.Client));
    }

    [Fact]
    public async Task Add_UnknownListing_Returns404()
    {
        var buyer = await _scenario.NewUserAsync("buyer");

        var response = await PostEmptyAsync(buyer.Client, FavoriteUrl(Guid.NewGuid()));

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task Add_AsGuest_Returns401()
    {
        var response = await PostEmptyAsync(_scenario.Anonymous(), FavoriteUrl(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Add_AsAdmin_Returns403()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);

        var response = await PostEmptyAsync(admin.Client, FavoriteUrl(id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Remove_Favorite_Returns204AndRemovesIt()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(buyer.Client, FavoriteUrl(id))).StatusCode);

        var response = await buyer.Client.DeleteAsync(FavoriteUrl(id), Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await FavoriteIdsAsync(buyer.Client));
        Assert.Empty((await FavoritesAsync(buyer.Client)).Items!);
        await AssertErrorAsync(await buyer.Client.DeleteAsync(FavoriteUrl(id), Ct), HttpStatusCode.NotFound, "FavoriteErrors.NotFound");
    }

    [Fact]
    public async Task Remove_FavoriteOfHiddenListing_StillWorks()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(buyer.Client, FavoriteUrl(id))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/deactivate")).StatusCode);

        var response = await buyer.Client.DeleteAsync(FavoriteUrl(id), Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task List_HiddenListing_IsLeftOutAndComesBackWhenVisibleAgain()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(buyer.Client, FavoriteUrl(id))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/deactivate")).StatusCode);

        Assert.Empty(await FavoriteIdsAsync(buyer.Client));
        var hidden = await FavoritesAsync(buyer.Client);
        Assert.Empty(hidden.Items!);
        Assert.Equal(0, hidden.TotalCount);

        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/reactivate")).StatusCode);

        Assert.Equal(new[] { id }, await FavoriteIdsAsync(buyer.Client));
        await AssertErrorAsync(await PostEmptyAsync(buyer.Client, FavoriteUrl(id)), HttpStatusCode.Conflict, "FavoriteErrors.AlreadyFavorite");
    }

    [Fact]
    public async Task List_SeveralFavorites_NewestFirstAndPaged()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var first = await CreateApprovedAsync(seller.Client, admin.Client);
        var second = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(buyer.Client, FavoriteUrl(first))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostEmptyAsync(buyer.Client, FavoriteUrl(second))).StatusCode);

        var all = await FavoritesAsync(buyer.Client);
        var pageTwo = await FavoritesAsync(buyer.Client, "page=2&pageSize=1");

        Assert.Equal(new[] { second, first }, Ids(all));
        Assert.Equal(new[] { first }, Ids(pageTwo));
        Assert.Equal(2, pageTwo.TotalCount);
        Assert.Equal(2, pageTwo.TotalPages);
    }

    [Theory]
    [InlineData("page=0", "Pagination.PageInvalid")]
    [InlineData("page=10001", "Pagination.PageInvalid")]
    [InlineData("pageSize=0", "Pagination.PageSizeInvalid")]
    public async Task List_PaginationOutOfRange_Returns400(string query, string code)
    {
        var buyer = await _scenario.NewUserAsync("buyer");

        var response = await buyer.Client.GetAsync($"{Routes.Favorites}?{query}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, code);
    }
}
