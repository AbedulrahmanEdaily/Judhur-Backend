using Asp.Versioning;

using Judhur.Application.Features.Favorites.Commands.AddFavorite;
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
}
