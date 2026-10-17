using System.Net;
using System.Text;

using Judhur.Api.IntegrationTests.Infrastructure;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Properties.Enums;

using static Judhur.Api.IntegrationTests.Marketplace.Market;

namespace Judhur.Api.IntegrationTests.Marketplace;

[Collection(ApiCollection.Name)]
public sealed class PropertyMediaTests(JudhurApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    private static string ImagesUrl(Guid propertyId) => $"{Routes.Properties}/{propertyId}/images";

    private static string DocumentUrl(Guid propertyId) => $"{Routes.Properties}/{propertyId}/ownership-document";

    [Fact]
    public async Task UploadImage_FirstImage_Returns200AndBecomesMain()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await seller.Client.PostAsync(ImagesUrl(id), Scenario.ImageForm(), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var image = await response.ReadAsync<PropertyImageDto>();
        Assert.True(image.IsMainImage);
        Assert.Equal(1, image.DisplayOrder);
        Assert.StartsWith($"{FilesHost}judhur/properties/{id}/", image.Url, StringComparison.Ordinal);
        Assert.Contains(ImagePublicId(image.Url), factory.Files.Files);
        var stored = Assert.Single((await GetMineAsync(seller.Client, id)).Images);
        Assert.Equal(image.Id, stored.Id);
    }

    [Fact]
    public async Task UploadImage_FlaggedMain_BecomesTheOnlyMainImage()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        await Scenario.AddImageAsync(seller.Client, id);

        var response = await seller.Client.PostAsync(ImagesUrl(id), Scenario.ImageForm(isMainImage: true), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var image = await response.ReadAsync<PropertyImageDto>();
        Assert.Equal(2, image.DisplayOrder);
        var main = Assert.Single((await GetMineAsync(seller.Client, id)).Images, i => i.IsMainImage);
        Assert.Equal(image.Id, main.Id);
    }

    [Theory]
    [InlineData("image/gif", "photo.gif")]
    [InlineData("application/pdf", "photo.pdf")]
    [InlineData("text/plain", "photo.txt")]
    public async Task UploadImage_UnsupportedContentType_Returns400(string contentType, string fileName)
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await seller.Client.PostAsync(ImagesUrl(id), Scenario.ImageForm(contentType: contentType, fileName: fileName), Ct);

        await AssertValidationErrorAsync(response, "ContentType");
        Assert.Empty((await GetMineAsync(seller.Client, id)).Images);
    }

    [Fact]
    public async Task UploadImage_ContentNotAnImage_Returns400InvalidFileContent()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        var content = Encoding.UTF8.GetBytes($"%{FakeFileStorage.InvalidContentMarker}%");

        var response = await seller.Client.PostAsync(ImagesUrl(id), Scenario.ImageForm(content: content), Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Storage.InvalidFileContent");
        Assert.Empty((await GetMineAsync(seller.Client, id)).Images);
    }

    [Fact]
    public async Task UploadImage_ListingOfAnotherSeller_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await other.Client.PostAsync(ImagesUrl(id), Scenario.ImageForm(), Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
        Assert.Empty((await GetMineAsync(seller.Client, id)).Images);
    }

    [Fact]
    public async Task UploadImage_BeyondMaximum_Returns400MaxImagesReached()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        for (var i = 0; i < 10; i++)
        {
            await Scenario.AddImageAsync(seller.Client, id);
        }

        var response = await seller.Client.PostAsync(ImagesUrl(id), Scenario.ImageForm(), Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "PropertyErrors.MaxImagesReached");
        Assert.Equal(10, (await GetMineAsync(seller.Client, id)).Images.Count);
    }

    [Fact]
    public async Task DeleteImage_MainImage_Returns400CannotRemoveMainImage()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        var main = await Scenario.AddImageAsync(seller.Client, id);
        await Scenario.AddImageAsync(seller.Client, id);

        var response = await seller.Client.DeleteAsync($"{ImagesUrl(id)}/{main}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "PropertyErrors.CannotRemoveMainImage");
        Assert.Equal(2, (await GetMineAsync(seller.Client, id)).Images.Count);
    }

    [Fact]
    public async Task DeleteImage_NormalImage_Returns204AndDeletesStoredFile()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        await Scenario.AddImageAsync(seller.Client, id);
        var second = await Scenario.AddImageAsync(seller.Client, id);
        var url = (await GetMineAsync(seller.Client, id)).Images.Single(i => i.Id == second).Url;

        var response = await seller.Client.DeleteAsync($"{ImagesUrl(id)}/{second}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain((await GetMineAsync(seller.Client, id)).Images, i => i.Id == second);
        Assert.Contains(ImagePublicId(url), factory.Files.Deleted);
    }

    [Fact]
    public async Task DeleteImage_UnknownImage_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        await Scenario.AddImageAsync(seller.Client, id);

        var response = await seller.Client.DeleteAsync($"{ImagesUrl(id)}/{Guid.NewGuid()}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.ImageNotFound");
    }

    [Fact]
    public async Task DeleteImage_ApprovedListingWithMinimumImages_Returns400MinImagesRequired()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        var normal = (await GetMineAsync(seller.Client, id)).Images.First(i => !i.IsMainImage);

        var response = await seller.Client.DeleteAsync($"{ImagesUrl(id)}/{normal.Id}", Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "PropertyErrors.MinImagesRequired");
        Assert.Equal(3, (await GetMineAsync(seller.Client, id)).Images.Count);
    }

    [Fact]
    public async Task SetMainImage_OtherImage_LeavesExactlyOneMainImage()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        var first = await Scenario.AddImageAsync(seller.Client, id);
        var second = await Scenario.AddImageAsync(seller.Client, id);

        var response = await seller.Client.PutAsync($"{ImagesUrl(id)}/{second}/main", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var images = (await GetMineAsync(seller.Client, id)).Images;
        var main = Assert.Single(images, i => i.IsMainImage);
        Assert.Equal(second, main.Id);
        Assert.False(images.Single(i => i.Id == first).IsMainImage);
    }

    [Fact]
    public async Task SetMainImage_UnknownImage_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        await Scenario.AddImageAsync(seller.Client, id);

        var response = await seller.Client.PutAsync($"{ImagesUrl(id)}/{Guid.NewGuid()}/main", null, Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.ImageNotFound");
    }

    [Fact]
    public async Task SetMainImage_ApprovedListing_SendsItBackToPending()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        var normal = (await GetMineAsync(seller.Client, id)).Images.First(i => !i.IsMainImage);

        var response = await seller.Client.PutAsync($"{ImagesUrl(id)}/{normal.Id}/main", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ModerationStatus.Pending, (await GetMineAsync(seller.Client, id)).ModerationStatus);
        Assert.Equal(HttpStatusCode.NotFound, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
    }

    [Fact]
    public async Task UploadDocument_Pdf_Returns204AndMarksDocumentPresent()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await seller.Client.PutAsync(DocumentUrl(id), Scenario.DocumentForm(), Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True((await GetMineAsync(seller.Client, id)).HasOwnershipDocument);
    }

    [Fact]
    public async Task UploadDocument_Photo_Returns204()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await seller.Client.PutAsync(DocumentUrl(id), Scenario.DocumentForm(contentType: "image/png", fileName: "deed.png"), Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True((await GetMineAsync(seller.Client, id)).HasOwnershipDocument);
    }

    [Fact]
    public async Task UploadDocument_UnsupportedFormat_Returns400()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await seller.Client.PutAsync(DocumentUrl(id), Scenario.DocumentForm(contentType: "text/plain", fileName: "deed.txt"), Ct);

        await AssertValidationErrorAsync(response, "FileName", "ContentType");
        Assert.False((await GetMineAsync(seller.Client, id)).HasOwnershipDocument);
    }

    [Fact]
    public async Task UploadDocument_ContentDoesNotMatchFormat_Returns400InvalidFileContent()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());
        var content = Encoding.UTF8.GetBytes(FakeFileStorage.InvalidContentMarker);

        var response = await seller.Client.PutAsync(DocumentUrl(id), Scenario.DocumentForm(content: content), Ct);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Storage.InvalidFileContent");
        Assert.False((await GetMineAsync(seller.Client, id)).HasOwnershipDocument);
    }

    [Fact]
    public async Task UploadDocument_ListingOfAnotherSeller_Returns404()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var other = await _scenario.NewUserAsync("other");
        var id = await Scenario.CreatePropertyAsync(seller.Client, new ListingBody());

        var response = await other.Client.PutAsync(DocumentUrl(id), Scenario.DocumentForm(), Ct);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "PropertyErrors.NotFound");
    }

    [Fact]
    public async Task UploadDocument_ReplacingOnApprovedListing_DeletesOldFileAndSendsItBackToPending()
    {
        var seller = await _scenario.NewUserAsync("seller");
        var admin = await _scenario.AdminAsync();
        var id = await CreateApprovedAsync(seller.Client, admin.Client);
        var oldDocument = DocumentPublicId((await GetForReviewAsync(admin.Client, id)).OwnershipDocumentUrl!);

        var response = await seller.Client.PutAsync(DocumentUrl(id), Scenario.DocumentForm(), Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(oldDocument, factory.Files.Deleted);
        var review = await GetForReviewAsync(admin.Client, id);
        Assert.NotEqual(oldDocument, DocumentPublicId(review.OwnershipDocumentUrl!));
        Assert.Equal(ModerationStatus.Pending, review.ModerationStatus);
        Assert.Equal(HttpStatusCode.NotFound, (await _scenario.Anonymous().GetAsync(PublicUrl(id), Ct)).StatusCode);
    }
}
