using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Conversations.Commands.MarkConversationAsRead;
using Judhur.Application.Features.Conversations.Commands.SendMessage;
using Judhur.Application.Features.Conversations.Commands.StartConversation;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Application.Features.Conversations.Queries.GetConversationById;
using Judhur.Application.Features.Conversations.Queries.GetConversationMessages;
using Judhur.Application.Features.Conversations.Queries.GetMyConversations;
using Judhur.Application.Features.Conversations.Queries.GetUnreadMessagesCount;
using Judhur.Contracts.Requests;
using Judhur.Domain.Common;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Judhur.Api.Controllers.Area.User;

[Area("User")]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[Area]/[controller]")]
[Authorize(Roles = Roles.User)]
public sealed class ConversationsController(ISender sender) : ApiController
{
    private const int DefaultMessagesLimit = 30;

    private readonly ISender _sender = sender;

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.SendMessage)]
    [ProducesResponseType(typeof(StartConversationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(StartConversationResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [EndpointSummary("Contacts the seller of a published property.")]
    [EndpointDescription("Sends a first message (up to 2000 characters) to the seller of an approved and active property. There is one conversation per buyer and property: if it already exists the message is added to it and 200 is returned with isNew=false, otherwise the conversation is created and 201 is returned. The seller receives the message in real time on the chat hub. Returns 404 if the property is not publicly visible and 409 ConversationErrors.CannotStartConversationWithSelf for the user's own listing. Shares the 30 messages per minute limit with sending messages.")]
    [EndpointName("StartConversation")]
    public async Task<IActionResult> StartAsync([FromBody] StartConversationCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(
            response => response.IsNew
                ? CreatedAtRoute("GetConversationById", new { version = "1", conversationId = response.ConversationId }, response)
                : Ok(response),
            Problem);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ConversationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Lists the current user's conversations.")]
    [EndpointDescription("Returns the inbox of the authenticated user as buyer and as seller, most recent activity first. Each item has the property (with isAvailable=false when it is no longer published), the other party, the user's role, a preview of the last message and the number of unread messages. Not cached.")]
    [EndpointName("GetMyConversations")]
    public async Task<IActionResult> GetMineAsync([FromQuery] PageRequest pageRequest, CancellationToken ct)
    {
        var result = await _sender.Send(new GetMyConversationsQuery(pageRequest.Page, pageRequest.PageSize), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadMessagesCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Counts the current user's unread messages.")]
    [EndpointDescription("Returns { count } of messages sent to the authenticated user that are not read yet, across all conversations, for the badge on the messages icon. Not cached.")]
    [EndpointName("GetUnreadMessagesCount")]
    public async Task<IActionResult> GetUnreadCountAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetUnreadMessagesCountQuery(), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{conversationId:guid}", Name = "GetConversationById")]
    [ProducesResponseType(typeof(ConversationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Gets the header of a conversation.")]
    [EndpointDescription("Returns the property, the other party and the user's role in the conversation. Returns 404 if the conversation does not exist or the user is not part of it.")]
    [EndpointName("GetConversationById")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid conversationId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetConversationByIdQuery(conversationId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(MessagesPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Gets the messages of a conversation, newest first.")]
    [EndpointDescription("Cursor pagination: without before returns the latest messages; to load older ones send before = the id of the oldest message already shown. limit is 1 to 100 (default 30). Items are newest first and hasMore says whether older messages exist. Returns 400 ConversationErrors.InvalidCursor if before is not a message of this conversation and 404 if the user is not part of it.")]
    [EndpointName("GetConversationMessages")]
    public async Task<IActionResult> GetMessagesAsync([FromRoute] Guid conversationId, [FromQuery] Guid? before, [FromQuery] int limit = DefaultMessagesLimit, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetConversationMessagesQuery(conversationId, before, limit), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{conversationId:guid}/messages")]
    [EnableRateLimiting(RateLimitPolicies.SendMessage)]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [EndpointSummary("Sends a message in a conversation.")]
    [EndpointDescription("Adds a message of up to 2000 characters and returns it. The other party receives it in real time on the chat hub (event messageReceived). Works even when the property is no longer published. Returns 404 if the user is not part of the conversation and 409 Conversations.ParticipantUnavailable when the other party was banned or deleted their account. Limited to 30 messages per minute per user.")]
    [EndpointName("SendMessage")]
    public async Task<IActionResult> SendMessageAsync([FromRoute] Guid conversationId, [FromBody] SendMessageCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request with { ConversationId = conversationId }, ct);
        return result.Match(response => Created((string?)null, response), Problem);
    }

    [HttpPost("{conversationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Marks the other party's messages as read.")]
    [EndpointDescription("Marks the unread messages from the other party in this conversation as read and notifies them in real time (event messagesRead). Send upTo with the id of the newest message the user has actually seen so that a message arriving at the same moment stays unread; without it every unread message is marked. Returns 204 also when nothing was unread, and 404 if the user is not part of the conversation or upTo is not a message of it.")]
    [EndpointName("MarkConversationAsRead")]
    public async Task<IActionResult> MarkAsReadAsync([FromRoute] Guid conversationId, [FromQuery] Guid? upTo, CancellationToken ct)
    {
        var result = await _sender.Send(new MarkConversationAsReadCommand(conversationId, upTo), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
