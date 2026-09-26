using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Properties.Commands.CreateProperty;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Application.Features.Properties.Queries.GetMyProperties;
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
    [HttpGet("{propertyId:guid}", Name = "GetPropertyById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a property by its ID.")]
    [EndpointDescription("Returns 404 if the property does not exist, is not approved, or is inactive.")]
    [EndpointName("GetPropertyById")]
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
    [EndpointDescription("Creates a property owned by the authenticated user, starting in Pending moderation.")]
    [EndpointName("CreateProperty")]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePropertyCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(response => CreatedAtRoute(routeName: "GetPropertyById", routeValues: new { version = "1", propertyId = response.Id }, value: response), Problem);
    }
}
