using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Notifications.Commands.MarkAllAsRead;
using Judhur.Application.Features.Notifications.Commands.MarkAsRead;
using Judhur.Application.Features.Notifications.Dto;
using Judhur.Application.Features.Notifications.Queries.GetMyNotifications;
using Judhur.Application.Features.Notifications.Queries.GetUnreadNotificationsCount;
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

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Counts the current user's unread notifications.")]
    [EndpointDescription("Returns { count } for the badge on the bell icon. Works for every role. Cheap enough to call on every page load or on a short interval; it is not cached, so the number is always current after a notification is read.")]
    [EndpointName("GetUnreadNotificationsCount")]
    public async Task<IActionResult> GetUnreadCountAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetUnreadNotificationsCountQuery(), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Marks one notification as read.")]
    [EndpointDescription("Marks a notification of the authenticated user as read. Calling it again on a notification that is already read also returns 204, so the client can call it every time a notification is opened. Returns 404 if the notification does not exist or belongs to another user.")]
    [EndpointName("MarkNotificationAsRead")]
    public async Task<IActionResult> MarkAsReadAsync([FromRoute] Guid notificationId, CancellationToken ct)
    {
        var result = await _sender.Send(new MarkAsReadCommand(notificationId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Marks all of the current user's notifications as read.")]
    [EndpointDescription("Marks every unread notification of the authenticated user as read in one request, for the «تعليم الكل كمقروء» button. Returns 204 even when there is nothing unread, so the client never has to check first. After it, the unread count is 0.")]
    [EndpointName("MarkAllNotificationsAsRead")]
    public async Task<IActionResult> MarkAllAsReadAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new MarkAllAsReadCommand(), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
