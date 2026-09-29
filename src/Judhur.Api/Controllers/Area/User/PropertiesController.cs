using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Commands.AddPropertyImage;
using Judhur.Application.Features.Properties.Commands.CreateProperty;
using Judhur.Application.Features.Properties.Commands.DeactivateProperty;
using Judhur.Application.Features.Properties.Commands.DeleteProperty;
using Judhur.Application.Features.Properties.Commands.DeletePropertyImage;
using Judhur.Application.Features.Properties.Commands.MarkPropertyAsRented;
using Judhur.Application.Features.Properties.Commands.MarkPropertyAsSold;
using Judhur.Application.Features.Properties.Commands.ReactivateProperty;
using Judhur.Application.Features.Properties.Commands.SetMainPropertyImage;
using Judhur.Application.Features.Properties.Commands.UpdatePropertyDescription;
using Judhur.Application.Features.Properties.Commands.UpdatePropertyDetails;
using Judhur.Application.Features.Properties.Commands.UploadOwnershipDocument;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Application.Features.Properties.Queries.GetMyProperties;
using Judhur.Application.Features.Properties.Queries.GetMyPropertyById;
using Judhur.Application.Features.Properties.Queries.GetProperties;
using Judhur.Application.Features.Properties.Queries.GetPropertyById;
using Judhur.Api.Mapping;
using Judhur.Contracts.Requests;
using Judhur.Domain.Common;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Controllers.Area.User;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = Roles.User)]
public sealed class PropertiesController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<PropertySummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a paginated list of properties.")]
    [EndpointDescription("Returns a paginated, filterable, and sortable list of published (approved and active) properties.")]
    [EndpointName("GetProperties")]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] PropertyFilterRequest filters,
        [FromQuery] PageRequest pageRequest,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetPropertiesQuery(
            pageRequest.Page,
            pageRequest.PageSize,
            filters.SearchTerm,
            filters.MinPrice,
            filters.MaxPrice,
            filters.City,
            filters.LandClassification.ToDomain(),
            filters.LegalStatus.ToDomain(),
            filters.PaymentType.ToDomain(),
            filters.PropertyStatus.ToDomain(),
            filters.PropertyType.ToDomain(),
            filters.SortColumn,
            filters.SortDirection
        ), ct);
        return result.Match(response => Ok(response), Problem);
    }
    [HttpGet("mine")]
    [ProducesResponseType(typeof(List<MyPropertyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves the current user's own properties.")]
    [EndpointDescription("Returns every property owned by the authenticated user in any moderation state (pending, approved, rejected) and whether active or not, newest first. Includes the rejection reason for rejected listings. Not cached.")]
    [EndpointName("GetMyProperties")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _sender.Send(new GetMyPropertiesQuery(), ct);
        return result.Match(response => Ok(response), Problem);
    }
    [HttpGet("mine/{propertyId:guid}")]
    [ProducesResponseType(typeof(MyPropertyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves one of the current user's own properties.")]
    [EndpointDescription("Returns the full details of a property owned by the authenticated user in any moderation state, including moderation status, rejection reason and the ownership document. Used to show the owner's listing and to pre-fill the edit form. Not cached. Returns 404 if the property does not exist or belongs to another user.")]
    [EndpointName("GetMyPropertyById")]
    public async Task<IActionResult> GetMineById([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetMyPropertyByIdQuery(propertyId), ct);
        return result.Match(response => Ok(response), Problem);
    }
    [HttpGet("{propertyId:guid}", Name = "GetPropertyById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a property by its ID.")]
    [EndpointDescription("Returns 404 if the property does not exist, is not approved, or is inactive.")]
    [EndpointName("GetPropertyById")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetPropertyByIdQuery(propertyId), ct);
        return result.Match(response => Ok(response), Problem);
    }
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Creates a new property listing.")]
    [EndpointDescription("Creates a property owned by the authenticated user, starting in Pending moderation. Upload the images and the ownership document afterwards; both are required before the listing can be approved.")]
    [EndpointName("CreateProperty")]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePropertyCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(response => CreatedAtRoute(routeName: "GetPropertyById", routeValues: new { version = "1", propertyId = response.Id }, value: response), Problem);
    }
    [HttpPut("{propertyId:guid}/details")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates a property's details.")]
    [EndpointDescription("Replaces the editable details of a property owned by the authenticated user (everything except the description, which has its own endpoint). Any change sends the listing back to Pending moderation, so it disappears from public search until an admin approves it again. Returns 404 if the property does not exist or belongs to another user.")]
    [EndpointName("UpdatePropertyDetails")]
    public async Task<IActionResult> UpdateDetailsAsync([FromRoute] Guid propertyId, [FromBody] UpdatePropertyDetailsCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request with { PropertyId = propertyId }, ct);
        return result.Match(_ => NoContent(), Problem);
    }
    [HttpPut("{propertyId:guid}/description")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates a property's description.")]
    [EndpointDescription("Replaces the description of a property owned by the authenticated user. Send null or an empty value to clear it. Unlike the details endpoint, this does not send the listing back to moderation. Returns 404 if the property does not exist or belongs to another user.")]
    [EndpointName("UpdatePropertyDescription")]
    public async Task<IActionResult> UpdateDescriptionAsync([FromRoute] Guid propertyId, [FromBody] UpdatePropertyDescriptionCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request with { PropertyId = propertyId }, ct);
        return result.Match(_ => NoContent(), Problem);
    }
    [HttpDelete("{propertyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Deletes a property.")]
    [EndpointDescription("Soft-deletes a property owned by the authenticated user: it disappears from search, details and the owner's list, while its conversations and reports are kept. Returns 404 if the property does not exist or belongs to another user.")]
    [EndpointName("DeleteProperty")]
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new DeletePropertyCommand(propertyId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
    [HttpPost("{propertyId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Deactivates a property.")]
    [EndpointDescription("Hides a property owned by the authenticated user from public search and details, without deleting it. Works in any moderation state. Returns 409 if the property is already inactive, 404 if it does not exist or belongs to another user.")]
    [EndpointName("DeactivateProperty")]
    public async Task<IActionResult> DeactivateAsync([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new DeactivatePropertyCommand(propertyId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
    [HttpPost("{propertyId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Reactivates a property.")]
    [EndpointDescription("Makes a deactivated property owned by the authenticated user visible again. Only approved properties can be reactivated, so this never skips moderation. Returns 409 if the property is already active or not approved, 404 if it does not exist or belongs to another user.")]
    [EndpointName("ReactivateProperty")]
    public async Task<IActionResult> ReactivateAsync([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new ReactivatePropertyCommand(propertyId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
    [HttpPost("{propertyId:guid}/mark-sold")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Marks a property as sold.")]
    [EndpointDescription("Changes the status of an approved property that is for sale to Sold. The listing stays visible with a sold badge. Returns 409 if the property is not approved or not for sale, 404 if it does not exist or belongs to another user.")]
    [EndpointName("MarkPropertyAsSold")]
    public async Task<IActionResult> MarkAsSoldAsync([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new MarkPropertyAsSoldCommand(propertyId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
    [HttpPost("{propertyId:guid}/mark-rented")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Marks a property as rented.")]
    [EndpointDescription("Changes the status of an approved property that is for rent to Rented. The listing stays visible with a rented badge. Returns 409 if the property is not approved or not for rent, 404 if it does not exist or belongs to another user.")]
    [EndpointName("MarkPropertyAsRented")]
    public async Task<IActionResult> MarkAsRentedAsync([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new MarkPropertyAsRentedCommand(propertyId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
    [HttpPost("{propertyId:guid}/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PropertyImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Uploads an image for a property.")]
    [EndpointDescription("Uploads a JPG, PNG or WEBP image (max 5 MB) to a property owned by the authenticated user. A property can have up to 10 images. The first image becomes the main image automatically; send isMainImage=true to make a later one the main image. Returns 404 if the property does not exist or belongs to another user.")]
    [EndpointName("AddPropertyImage")]
    public async Task<IActionResult> AddImageAsync([FromRoute] Guid propertyId, IFormFile file, [FromForm] bool isMainImage, CancellationToken ct)
    {
        await using var content = file.OpenReadStream();
        var result = await _sender.Send(new AddPropertyImageCommand(
            propertyId,
            content,
            file.FileName,
            file.ContentType,
            file.Length,
            isMainImage), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{propertyId:guid}/images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Deletes an image from a property.")]
    [EndpointDescription("Deletes an image from a property owned by the authenticated user and removes the file from storage. The main image cannot be deleted; set another image as main first. An approved property must keep at least 3 images. Returns 404 if the property or image does not exist.")]
    [EndpointName("DeletePropertyImage")]
    public async Task<IActionResult> DeleteImageAsync([FromRoute] Guid propertyId, [FromRoute] Guid imageId, CancellationToken ct)
    {
        var result = await _sender.Send(new DeletePropertyImageCommand(propertyId, imageId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPut("{propertyId:guid}/images/{imageId:guid}/main")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Sets the main image of a property.")]
    [EndpointDescription("Makes the given image the main image of a property owned by the authenticated user; the previous main image becomes a normal image. Returns 404 if the property or image does not exist.")]
    [EndpointName("SetMainPropertyImage")]
    public async Task<IActionResult> SetMainImageAsync([FromRoute] Guid propertyId, [FromRoute] Guid imageId, CancellationToken ct)
    {
        var result = await _sender.Send(new SetMainPropertyImageCommand(propertyId, imageId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPut("{propertyId:guid}/ownership-document")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Uploads or replaces a property's ownership document.")]
    [EndpointDescription("Uploads the ownership document of a property owned by the authenticated user, as a PDF or a photo (JPG, PNG or WEBP, max 10 MB). The file is stored privately and only admins can view it through a temporary link. Replacing the document of an approved or rejected property sends it back to moderation. Returns 404 if the property does not exist or belongs to another user.")]
    [EndpointName("UploadOwnershipDocument")]
    public async Task<IActionResult> UploadOwnershipDocumentAsync([FromRoute] Guid propertyId, IFormFile file, CancellationToken ct)
    {
        await using var content = file.OpenReadStream();
        var result = await _sender.Send(new UploadOwnershipDocumentCommand(
            propertyId,
            content,
            file.FileName,
            file.ContentType,
            file.Length), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}