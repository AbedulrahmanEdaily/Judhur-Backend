using Asp.Versioning;

using Judhur.Application.Features.Stats.Dto;
using Judhur.Application.Features.Stats.Queries.GetAdminStats;
using Judhur.Domain.Common;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judhur.Api.Controllers.Area.Admin;

[Area("Admin")]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[Area]/[controller]")]
[Authorize(Roles = Roles.Admin)]
public sealed class StatsController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(AdminStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Gets the admin dashboard numbers.")]
    [EndpointDescription("User counts (admins excluded), listing counts by state, pending reports and activity over the last seven days. properties.waitingForReview matches the moderation queue; properties.incomplete counts pending listings still missing images or the ownership document.")]
    [EndpointName("GetAdminStats")]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminStatsQuery(), ct);
        return result.Match(response => Ok(response), Problem);
    }
}
