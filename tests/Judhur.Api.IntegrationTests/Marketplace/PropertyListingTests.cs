using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Properties.Enums;

using static Judhur.Api.IntegrationTests.Marketplace.Market;

namespace Judhur.Api.IntegrationTests.Marketplace;

[Collection(ApiCollection.Name)]
public sealed class PropertyListingTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Create_ValidListing_Returns201WithLocationOfOwnerView()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var body = new ListingBody();

        var response = await seller.Client.PostJsonAsync(Routes.Properties, body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.ReadAsync<PropertyDto>();
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(body.Title, created.Title);
        Assert.Empty(created.Images);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/v1/User/Properties/mine/{created.Id}", response.Headers.Location.OriginalString, StringComparison.OrdinalIgnoreCase);

        var owned = await seller.Client.GetAsync(response.Headers.Location, Ct);

        Assert.Equal(HttpStatusCode.OK, owned.StatusCode);
        var details = await owned.ReadAsync<MyPropertyDetailsDto>();
        Assert.Equal(ModerationStatus.Pending, details.ModerationStatus);
        Assert.True(details.IsActive);
        Assert.False(details.HasOwnershipDocument);
        Assert.Equal(body.Price, details.Price);
        Assert.Equal(body.City, details.City);
    }

    [Theory]
    [InlineData("EmptyTitle", "Title")]
    [InlineData("LongTitle", "Title")]
    [InlineData("ZeroPrice", "Price")]
    [InlineData("PriceScale", "Price")]
    [InlineData("AreaScale", "Area")]
    [InlineData("EmptyCity", "City")]
    [InlineData("EmptyAddress", "FullAddress")]
    [InlineData("Latitude", "Latitude")]
    [InlineData("Longitude", "Longitude")]
    [InlineData("SoldStatus", "PropertyStatus")]
    public async Task Create_InvalidField_Returns400WithFieldKey(string scenario, string expectedKey)
    {
        var seller = await _scenario.NewUserAsync("seller");
        var valid = new ListingBody();
        var body = scenario switch
        {
            "EmptyTitle" => valid with { Title = "" },
            "LongTitle" => valid with { Title = new string('ع', 201) },
            "ZeroPrice" => valid with { Price = 0m },
            "PriceScale" => valid with { Price = 120_000.125m },
            "AreaScale" => valid with { Area = 140.555m },
            "EmptyCity" => valid with { City = "" },
            "EmptyAddress" => valid with { FullAddress = "" },
            "Latitude" => valid with { Latitude = 91 },
            "Longitude" => valid with { Longitude = -181 },
            "SoldStatus" => valid with { PropertyStatus = PropertyStatus.Sold },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var response = await seller.Client.PostJsonAsync(Routes.Properties, body);

        await AssertValidationErrorAsync(response, expectedKey);
        Assert.Empty(await GetMyListAsync(seller.Client));
    }

    [Fact]
    public async Task Create_AsAdmin_Returns403()
    {
        var admin = await _scenario.AdminAsync();

        var response = await admin.Client.PostJsonAsync(Routes.Properties, new ListingBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsGuest_Returns401()
    {
        var response = await _scenario.Anonymous().PostJsonAsync(Routes.Properties, new ListingBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_SeveralSellers_ReturnsOnlyOwnListingsNewestFirst()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var first = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        var second = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        var foreign = await Scenario.CreatePropertyAsync(other.Client, new ListingBody());

        var mine = await GetMyListAsync(seller.Client);

        Assert.Equal(new[] { second, first }, mine.Select(p => p.Id));
        Assert.DoesNotContain(mine, p => p.Id == foreign);
        Assert.All(mine, p => Assert.Equal(ModerationStatus.Pending, p.ModerationStatus));
    }

    [Fact]
    public async Task GetMineById_ListingOfAnotherSeller_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await other.Client.GetAsync(MineUrl(id), Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task GetMineById_OwnListing_ReturnsImagesInDisplayOrder()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());

        var details = await GetMineAsync(seller.Client, id);

        Assert.Equal(3, details.Images.Count);
        Assert.Equal(new[] { 1, 2, 3 }, details.Images.Select(i => i.DisplayOrder));
        Assert.Single(details.Images, i => i.IsMainImage);
        Assert.True(details.Images[0].IsMainImage);
        Assert.True(details.HasOwnershipDocument);
    }

    [Fact]
    public async Task GetPublic_PendingListing_Returns404ForGuestAndOwner()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());

        var guest = await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct);
        var owner = await seller.Client.GetAsync(PublicUrl(id), Ct);

        await AssertErrorAsync(guest, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
        await AssertErrorAsync(owner, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task GetPublic_ApprovedListing_HidesSellerPhoneFromGuests()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var buyer = await _scenario.NewUserAsync("buyer");
        var admin = await _scenario.AdminAsync();
        var body = new ListingBody();
        var id = await CreateApprovedAsync(seller.Client, admin.Client, body);

        var guest = await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct);
        var member = await buyer.Client.GetAsync(PublicUrl(id), Ct);

        Assert.Equal(HttpStatusCode.OK, guest.StatusCode);
        var guestJson = await guest.ReadJsonAsync();
        Assert.Equal(body.Title, guestJson.GetProperty("title").GetString());
        Assert.Equal(3, guestJson.GetProperty("images").GetArrayLength());
        var guestSeller = guestJson.GetProperty("user");
        Assert.Equal(seller.Id, guestSeller.GetProperty("id").GetGuid());
        Assert.True(IsNullOrMissing(guestSeller, "phoneNumber"));

        Assert.Equal(HttpStatusCode.OK, member.StatusCode);
        var memberSeller = (await member.ReadJsonAsync()).GetProperty("user");
        Assert.Equal(seller.Id, memberSeller.GetProperty("id").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(memberSeller.GetProperty("phoneNumber").GetString()));
    }

    [Fact]
    public async Task UpdateDetails_ApprovedListing_SendsItBackToPending()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var original = new ListingBody();
        var id = await CreateApprovedAsync(seller.Client, admin.Client, original);
        Assert.Equal(HttpStatusCode.OK, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
        var changed = original with { Title = NewTitle("عنوان معدل"), Price = 99_500.50m, City = "جنين" };

        var response = await seller.Client.PutJsonAsync($"{Routes.Properties}/{id}/details", changed.Details());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await GetMineAsync(seller.Client, id);
        Assert.Equal(ModerationStatus.Pending, details.ModerationStatus);
        Assert.Null(details.ReviewedAtUtc);
        Assert.Equal(changed.Title, details.Title);
        Assert.Equal(changed.Price, details.Price);
        Assert.Equal("جنين", details.City);
        Assert.Equal(original.Description, details.Description);
        var publicResponse = await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct);
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateDetails_PriceWithThreeDecimals_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var body = new ListingBody();
        var id = await Scenario.CreatePropertyAsync(seller.Client, body);

        var response = await seller.Client.PutJsonAsync($"{Routes.Properties}/{id}/details", (body with { Price = 10.555m }).Details());

        await AssertValidationErrorAsync(response, "Price");
        Assert.Equal(body.Price, (await GetMineAsync(seller.Client, id)).Price);
    }

    [Fact]
    public async Task UpdateDetails_ListingOfAnotherSeller_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var body = new ListingBody();
        var id = await Scenario.CreatePropertyAsync(seller.Client, body);

        var response = await other.Client.PutJsonAsync($"{Routes.Properties}/{id}/details", (body with { Price = 1m }).Details());

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task UpdateDescription_ApprovedListing_StaysApprovedAndRefreshesPublicView()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.OK, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
        const string description = "وصف جديد بعد التعديل مع تفاصيل إضافية";

        var response = await seller.Client.PutJsonAsync($"{Routes.Properties}/{id}/description", new { description });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ModerationStatus.Approved, (await GetMineAsync(seller.Client, id)).ModerationStatus);
        var publicView = await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct);
        Assert.Equal(HttpStatusCode.OK, publicView.StatusCode);
        Assert.Equal(description, (await publicView.ReadJsonAsync()).GetProperty("description").GetString());
    }

    [Fact]
    public async Task UpdateDescription_Null_ClearsDescription()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await seller.Client.PutJsonAsync($"{Routes.Properties}/{id}/description", new { description = (string?)null });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null((await GetMineAsync(seller.Client, id)).Description);
    }

    [Fact]
    public async Task UpdateDescription_TooLong_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await seller.Client.PutJsonAsync($"{Routes.Properties}/{id}/description", new { description = new string('و', 2001) });

        await AssertValidationErrorAsync(response, "Description");
    }

    [Fact]
    public async Task Delete_ApprovedListing_HidesItEverywhere()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.OK, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
        Assert.Contains(id, Ids(await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}")));

        var response = await seller.Client.DeleteAsync($"{Routes.Properties}/{id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await seller.Client.GetAsync(MineUrl(id), Ct)).StatusCode);
        Assert.DoesNotContain(await GetMyListAsync(seller.Client), p => p.Id == id);
        Assert.DoesNotContain(id, Ids(await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}")));
    }

    [Fact]
    public async Task Delete_AlreadyDeleted_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        Assert.Equal(HttpStatusCode.NoContent, (await seller.Client.DeleteAsync($"{Routes.Properties}/{id}", Ct)).StatusCode);

        var response = await seller.Client.DeleteAsync($"{Routes.Properties}/{id}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task Delete_ListingOfAnotherSeller_Returns404AndKeepsIt()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await other.Client.DeleteAsync($"{Routes.Properties}/{id}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
        Assert.Equal(id, (await GetMineAsync(seller.Client, id)).Id);
    }

    [Fact]
    public async Task DeactivateAndReactivate_ApprovedListing_TogglesPublicVisibility()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        Assert.Equal(HttpStatusCode.OK, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);

        var deactivate = await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/deactivate");

        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        Assert.False((await GetMineAsync(seller.Client, id)).IsActive);
        Assert.Equal(HttpStatusCode.NotFound, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
        await AssertErrorAsync(await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/deactivate"), HttpStatusCode.Conflict, "PropertyErrors.AlreadyDeactivated");

        var reactivate = await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/reactivate");

        Assert.Equal(HttpStatusCode.NoContent, reactivate.StatusCode);
        var details = await GetMineAsync(seller.Client, id);
        Assert.True(details.IsActive);
        Assert.Equal(ModerationStatus.Approved, details.ModerationStatus);
        Assert.Equal(HttpStatusCode.OK, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
        await AssertErrorAsync(await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/reactivate"), HttpStatusCode.Conflict, "PropertyErrors.AlreadyActive");
    }

    [Fact]
    public async Task Deactivate_ListingOfAnotherSeller_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await PostEmptyAsync(other.Client, $"{Routes.Properties}/{id}/deactivate");

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
        Assert.True((await GetMineAsync(seller.Client, id)).IsActive);
    }

    [Fact]
    public async Task Resubmit_RejectedListing_ReturnsItToPendingWithoutReason()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await Scenario.CreateReadyPropertyAsync(seller.Client, new ListingBody());
        await RejectAsync(admin.Client, id, "الصور غير واضحة");
        var rejected = await GetMineAsync(seller.Client, id);
        Assert.Equal(ModerationStatus.Rejected, rejected.ModerationStatus);
        Assert.Equal("الصور غير واضحة", rejected.RejectionReason);

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/resubmit");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await GetMineAsync(seller.Client, id);
        Assert.Equal(ModerationStatus.Pending, details.ModerationStatus);
        Assert.Null(details.RejectionReason);
        Assert.Null(details.ReviewedAtUtc);
        Assert.NotNull(await FindPendingAsync(admin.Client, id));
    }

    [Fact]
    public async Task Resubmit_PendingListing_Returns409()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/resubmit");

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "PropertyErrors.CannotResubmitUnrejectedProperty");
    }

    [Fact]
    public async Task MarkSold_ApprovedListingForSale_ShowsSoldPublicly()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { PropertyStatus = PropertyStatus.ForSale });
        Assert.Equal(HttpStatusCode.OK, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/mark-sold");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var publicView = await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct);
        Assert.Equal(HttpStatusCode.OK, publicView.StatusCode);
        Assert.Equal("Sold", (await publicView.ReadJsonAsync()).GetProperty("propertyStatus").GetString());
        var listed = Assert.Single((await SearchAsync(_scenario.Anonymous(), $"sellerId={seller.Id}")).Items!);
        Assert.Equal(PropertyStatus.Sold, listed.PropertyStatus);
        await AssertErrorAsync(await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/mark-sold"), HttpStatusCode.Conflict, "PropertyErrors.PropertyStatusTransitionInvalid");
    }

    [Fact]
    public async Task MarkRented_ApprovedListingForRent_ChangesStatusToRented()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { PropertyStatus = PropertyStatus.ForRent });

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/mark-rented");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = await GetMineAsync(seller.Client, id);
        Assert.Equal(PropertyStatus.Rented, details.PropertyStatus);
        Assert.Equal(ModerationStatus.Approved, details.ModerationStatus);
    }

    [Fact]
    public async Task MarkRented_ListingForSale_Returns409()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client, new ListingBody { PropertyStatus = PropertyStatus.ForSale });

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/mark-rented");

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "PropertyErrors.PropertyStatusTransitionInvalid");
        Assert.Equal(PropertyStatus.ForSale, (await GetMineAsync(seller.Client, id)).PropertyStatus);
    }

    [Fact]
    public async Task MarkSold_PendingListing_Returns409()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await PostEmptyAsync(seller.Client, $"{Routes.Properties}/{id}/mark-sold");

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "PropertyErrors.CannotChangeStatusOfUnapprovedProperty");
    }
}
