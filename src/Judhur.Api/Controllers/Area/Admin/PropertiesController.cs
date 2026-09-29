using Asp.Versioning;

using Judhur.Application.Common.Models;
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
}