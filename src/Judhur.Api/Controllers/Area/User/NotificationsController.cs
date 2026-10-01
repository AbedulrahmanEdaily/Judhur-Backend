using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Notifications.Dto;
using Judhur.Application.Features.Notifications.Queries.GetMyNotifications;
using Judhur.Contracts.Requests;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Controllers.Area.User;

[Area("User")]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[Area]/[controller]")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Lists the current user's notifications.")]
    [EndpointDescription("Returns the authenticated user's notifications, newest first, read and unread together. Works for every role. Each item has a type (for example PropertyApproved or PropertyRejected) and an optional referenceId: for property notifications it is the property id, so the client can open the listing. Not cached.")]
    [EndpointName("GetMyNotifications")]
    public async Task<IActionResult> GetMineAsync([FromQuery] PageRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new GetMyNotificationsQuery(request.Page, request.PageSize), ct);
        return result.Match(response => Ok(response), Problem);
    }
}
