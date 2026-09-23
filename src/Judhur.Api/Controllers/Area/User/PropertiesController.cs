using Asp.Versioning;

using Judhur.Application.Features.Properties.Commands.CreateProperty;
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

    [HttpGet("{propertyId:guid}",Name ="GetPropertyById")]
    [EndpointName("GetPropertyById")]
    public IActionResult Get(Guid propertyId)
    {
        return Ok(propertyId);
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
