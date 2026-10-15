using Asp.Versioning;

using Judhur.Application.Features.Reviews.Commands.CreateReview;
using Judhur.Application.Features.Reviews.Commands.DeleteReview;
using Judhur.Application.Features.Reviews.Commands.UpdateReview;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Application.Features.Reviews.Queries.GetMyReviewStatus;
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
public sealed class ReviewsController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpGet("sellers/{sellerId:guid}/mine")]
    [ProducesResponseType(typeof(MyReviewStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Tells whether the current user can review a seller.")]
    [EndpointDescription("Returns { canReview, review }. review is the user's existing review of this seller, if any (edit or delete it). canReview is true only when the user has no review yet and has a conversation with this seller in which the seller replied. Use it to decide which form to show on the seller page.")]
    [EndpointName("GetMyReviewStatus")]
    public async Task<IActionResult> GetMyStatusAsync([FromRoute] Guid sellerId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetMyReviewStatusQuery(sellerId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.CreateReview)]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [EndpointSummary("Reviews a seller.")]
    [EndpointDescription("Creates a review with a rating from 0.5 to 5 in steps of 0.5 and an optional comment of up to 500 characters. Allowed only after a conversation with the seller in which the seller replied (409 ReviewErrors.NotEligible), once per seller (409 ReviewErrors.DuplicateReview; edit the existing one instead) and never for yourself (409 ReviewErrors.CannotReviewSelf). The seller is notified. Limited to 5 new reviews per hour per user.")]
    [EndpointName("CreateReview")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateReviewCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(response => Created((string?)null, response), Problem);
    }

    [HttpPut("{reviewId:guid}")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Edits the current user's review.")]
    [EndpointDescription("Replaces the rating and the comment of a review written by the authenticated user. An empty comment clears it. Returns 404 if the review does not exist or belongs to someone else.")]
    [EndpointName("UpdateReview")]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid reviewId, [FromBody] UpdateReviewCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request with { ReviewId = reviewId }, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{reviewId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Deletes the current user's review.")]
    [EndpointDescription("Deletes a review written by the authenticated user; the user can then review the seller again. Returns 404 if the review does not exist or belongs to someone else.")]
    [EndpointName("DeleteReview")]
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid reviewId, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteReviewCommand(reviewId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
