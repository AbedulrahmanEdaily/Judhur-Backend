using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Reviews.Dto;
using Judhur.Application.Features.Reviews.Queries.GetSellerReviews;
using Judhur.Application.Features.Sellers.Dto;
using Judhur.Application.Features.Sellers.Queries.GetSellerProfile;
using Judhur.Contracts.Requests;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Controllers.Area.User;

[Area("User")]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[Area]/[controller]")]
[AllowAnonymous]
public sealed class SellersController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpGet("{sellerId:guid}")]
    [ProducesResponseType(typeof(SellerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Gets the public profile of a seller.")]
    [EndpointDescription("Returns the public profile of a seller: full name, profile image, city, bio, registration date, the number of published (approved and active) properties, and the average rating (null without reviews) with the number of reviews. Email and phone number are never included. Available without signing in and cached for up to 10 minutes; profile changes and listing changes refresh it. Use GET /properties?sellerId={sellerId} for the seller's listings. Returns 404 if the user does not exist or is an administrator.")]
    [EndpointName("GetSellerProfile")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid sellerId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetSellerProfileQuery(sellerId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{sellerId:guid}/reviews")]
    [ProducesResponseType(typeof(PaginatedList<SellerReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Lists the reviews of a seller.")]
    [EndpointDescription("Returns the reviews written about a seller, newest first, with the reviewer's name and picture only (no id or contact details) and whether the review was edited. Available without signing in. The average and the count are on the seller profile and on the property details.")]
    [EndpointName("GetSellerReviews")]
    public async Task<IActionResult> GetReviewsAsync([FromRoute] Guid sellerId, [FromQuery] PageRequest pageRequest, CancellationToken ct)
    {
        var result = await _sender.Send(new GetSellerReviewsQuery(sellerId, pageRequest.Page, pageRequest.PageSize), ct);
        return result.Match(response => Ok(response), Problem);
    }
}
