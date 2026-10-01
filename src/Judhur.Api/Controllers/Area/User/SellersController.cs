using Asp.Versioning;

using Judhur.Application.Features.Sellers.Dto;
using Judhur.Application.Features.Sellers.Queries.GetSellerProfile;

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
    [EndpointDescription("Returns the public profile of a seller: full name, profile image, city, bio, registration date and the number of published (approved and active) properties. Email and phone number are never included. Available without signing in and cached for up to 10 minutes; profile changes and listing changes refresh it. Use GET /properties?sellerId={sellerId} for the seller's listings. Returns 404 if the user does not exist or is an administrator.")]
    [EndpointName("GetSellerProfile")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid sellerId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetSellerProfileQuery(sellerId), ct);
        return result.Match(response => Ok(response), Problem);
    }
}
