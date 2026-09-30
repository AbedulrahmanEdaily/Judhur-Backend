using Asp.Versioning;

using Judhur.Application.Features.Favorites.Commands.AddFavorite;
using Judhur.Application.Features.Favorites.Commands.RemoveFavorite;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Favorites.Queries.GetMyFavorites;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Contracts.Requests;
using Judhur.Domain.Common;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Controllers.Area.User;

[Area("User")]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[Area]/[controller]")]
[Authorize(Roles = Roles.User)]
public sealed class FavoritesController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpPost("{propertyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Adds a property to the user's favorites.")]
    [EndpointDescription("Adds a publicly visible property to the authenticated user's favorites. Returns 404 if the property does not exist, is not approved or is inactive, and 409 if it is already in the user's favorites.")]
    [EndpointName("AddFavorite")]
    public async Task<IActionResult> AddAsync([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new AddFavoriteCommand(propertyId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpDelete("{propertyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Removes a property from the user's favorites.")]
    [EndpointDescription("Removes a property from the authenticated user's favorites. Works even if the property is no longer publicly visible. Returns 404 if the property is not in the user's favorites.")]
    [EndpointName("RemoveFavorite")]
    public async Task<IActionResult> RemoveAsync([FromRoute] Guid propertyId, CancellationToken ct)
    {
        var result = await _sender.Send(new RemoveFavoriteCommand(propertyId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<PropertySummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Lists the user's favorite properties.")]
    [EndpointDescription("Returns the authenticated user's favorite properties, most recently added first, in the same shape as the public search results. Properties that are no longer publicly visible are left out but stay in the favorites, so they reappear if the property becomes visible again.")]
    [EndpointName("GetMyFavorites")]
    public async Task<IActionResult> GetMineAsync([FromQuery] PageRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new GetMyFavoritesQuery(request.Page, request.PageSize), ct);
        return result.Match(response => Ok(response), Problem);
    }
}
