using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Users.Commands.BanUser;
using Judhur.Application.Features.Users.Commands.UnbanUser;
using Judhur.Application.Features.Users.Dto;
using Judhur.Application.Features.Users.Queries.GetUserById;
using Judhur.Application.Features.Users.Queries.GetUsers;
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
public sealed class UsersController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Lists users for the admin panel.")]
    [EndpointDescription("Searches by name, email or phone (optional, up to 100 characters) and filters by status: All (default), Active, Banned or Unconfirmed. Newest accounts first. Each item carries the ban details and counts of the user's listings, active listings and pending reports on them.")]
    [EndpointName("GetUsers")]
    public async Task<IActionResult> GetAsync([FromQuery] PageRequest pageRequest, [FromQuery] string? search, [FromQuery] AdminUserStatus status = AdminUserStatus.All, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetUsersQuery(search, status, pageRequest.Page, pageRequest.PageSize), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Gets one user for the admin panel.")]
    [EndpointDescription("Returns the same shape as an item of the users list.")]
    [EndpointName("GetUserById")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid userId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetUserByIdQuery(userId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{userId:guid}/ban")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Bans a user.")]
    [EndpointDescription("Body: reason (required, up to 500 characters). Signs the user out of every session, deactivates their active listings and notifies them. They cannot log in or refresh until unbanned. Returns 409 for an admin account, the caller's own account or an already banned user.")]
    [EndpointName("BanUser")]
    public async Task<IActionResult> BanAsync([FromRoute] Guid userId, [FromBody] BanUserCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request with { UserId = userId }, ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{userId:guid}/unban")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Lifts a user's ban.")]
    [EndpointDescription("The user can log in again. Listings deactivated by the ban stay inactive; the user reactivates them. Returns 409 Users.NotBanned if the user is not banned.")]
    [EndpointName("UnbanUser")]
    public async Task<IActionResult> UnbanAsync([FromRoute] Guid userId, CancellationToken ct)
    {
        var result = await _sender.Send(new UnbanUserCommand(userId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
