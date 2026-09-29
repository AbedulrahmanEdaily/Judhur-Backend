using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Commands.ApproveProperty;
using Judhur.Application.Features.Properties.Commands.RejectProperty;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Application.Features.Properties.Queries.GetPendingProperties;
using Judhur.Application.Features.Properties.Queries.GetPropertyForReview;
using Judhur.Contracts.Requests;
using Judhur.Domain.Common;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Controllers.Area.Admin;

[Area("Admin")]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[Area]/[controller]")]
[Authorize(Roles = Roles.Admin)]
public sealed class PropertiesController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpGet("pending")]
    [ProducesResponseType(typeof(PaginatedList<PendingPropertyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Lists properties waiting for review.")]
    [EndpointDescription("Returns pending properties that are ready for review (at least 3 images, a main image and an ownership document), oldest first.")]
    [EndpointName("GetPendingProperties")]
    public async Task<IActionResult> GetPendingAsync([FromQuery] PageRequest pageRequest, CancellationToken ct)
    {
        var result = await _sender.Send(new GetPendingPropertiesQuery(pageRequest.Page, pageRequest.PageSize), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{propertyId:guid}")]
    [ProducesResponseType(typeof(PropertyForReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Gets a property with everything needed to review it.")]
    [EndpointDescription("Returns the full property at any moderation status, with its images, seller info and a link to the ownership document that expires after 10 minutes. The document fields are null when no document has been uploaded.")]
    [EndpointName("GetPropertyForReview")]
    public async Task<IActionResult> GetForReviewAsync([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetPropertyForReviewQuery(propertyId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{propertyId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Approves a property.")]
    [EndpointDescription("Approves a property so it becomes visible in public search and details. Returns 400 if the property has fewer than 3 images, no main image or no ownership document, 409 if it is already approved, and 404 if it does not exist.")]
    [EndpointName("ApproveProperty")]
    public async Task<IActionResult> ApproveAsync([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new ApprovePropertyCommand(propertyId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{propertyId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Rejects a property.")]
    [EndpointDescription("Rejects a property with a reason of up to 500 characters that the owner sees on their listing. Returns 400 if the reason is missing or too long, 409 if the property is already approved or already rejected, and 404 if it does not exist.")]
    [EndpointName("RejectProperty")]
    public async Task<IActionResult> RejectAsync([FromRoute] Guid propertyId, [FromBody] RejectPropertyCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request with { PropertyId = propertyId }, ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
