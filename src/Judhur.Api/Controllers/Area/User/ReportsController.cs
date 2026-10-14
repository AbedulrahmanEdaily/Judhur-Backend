using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Reports.Commands.CreateReport;
using Judhur.Application.Features.Reports.Dto;
using Judhur.Application.Features.Reports.Queries.GetMyReports;
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
public sealed class ReportsController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.CreateReport)]
    [ProducesResponseType(typeof(ReportCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [EndpointSummary("Reports a published property.")]
    [EndpointDescription("Creates a report on an approved and active property with a reason (Fraud, IncorrectInformation, Duplicate, InappropriateContent, Other) and optional details of up to 1000 characters; details are required for Other. Returns 404 if the property is not publicly visible, 409 ReportErrors.CannotReportOwnProperty for the user's own listing and 409 ReportErrors.DuplicateReport while an earlier report by the same user on the same property is still pending. Limited to 10 reports per hour per user.")]
    [EndpointName("CreateReport")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateReportCommand request, CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        return result.Match(response => Created((string?)null, response), Problem);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(PaginatedList<MyReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Lists the reports of the current user.")]
    [EndpointDescription("Returns the authenticated user's reports, newest first, with their status and the admin note once reviewed. Reports on properties that were deleted afterwards are still listed. Not cached.")]
    [EndpointName("GetMyReports")]
    public async Task<IActionResult> GetMineAsync([FromQuery] PageRequest pageRequest, CancellationToken ct)
    {
        var result = await _sender.Send(new GetMyReportsQuery(pageRequest.Page, pageRequest.PageSize), ct);
        return result.Match(response => Ok(response), Problem);
    }
}
