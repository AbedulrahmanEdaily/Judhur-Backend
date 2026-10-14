using Asp.Versioning;

using Judhur.Application.Common.Models;
using Judhur.Application.Features.Reports.Commands.DismissReport;
using Judhur.Application.Features.Reports.Commands.ResolveReport;
using Judhur.Application.Features.Reports.Dto;
using Judhur.Application.Features.Reports.Queries.GetReportById;
using Judhur.Application.Features.Reports.Queries.GetReports;
using Judhur.Contracts.Requests;
using Judhur.Domain.Common;
using Judhur.Domain.Reports.Enums;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Judhur.Api.Controllers.Area.Admin;

[Area("Admin")]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[Area]/[controller]")]
[Authorize(Roles = Roles.Admin)]
public sealed class ReportsController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ReportSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Lists reports by status.")]
    [EndpointDescription("Returns reports with the given status (Pending by default). Pending reports are ordered oldest first, reviewed ones by review date, newest first. Each item carries the property title, whether the property was deleted, and how many pending reports the property has.")]
    [EndpointName("GetReports")]
    public async Task<IActionResult> GetAsync([FromQuery] PageRequest pageRequest, [FromQuery] ReportStatus status = ReportStatus.Pending, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetReportsQuery(status, pageRequest.Page, pageRequest.PageSize), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{reportId:guid}")]
    [ProducesResponseType(typeof(ReportDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Gets a report with everything needed to review it.")]
    [EndpointDescription("Returns the report, the reporter's name, phone and picture, a summary of the reported property (also when it was deleted) and the number of other pending reports on the same property. Open the property review page with property.id to see the full listing.")]
    [EndpointName("GetReportById")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid reportId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetReportByIdQuery(reportId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{reportId:guid}/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Dismisses a report.")]
    [EndpointDescription("Closes this report only. The body is optional: an optional note of up to 1000 characters that the reporter sees. The reporter is notified. Other reports on the same property stay pending. Returns 409 ReportErrors.AlreadyReviewed if the report was already reviewed.")]
    [EndpointName("DismissReport")]
    public async Task<IActionResult> DismissAsync([FromRoute] Guid reportId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DismissReportCommand? request, CancellationToken ct)
    {
        var command = (request ?? new DismissReportCommand(reportId, null)) with { ReportId = reportId };
        var result = await _sender.Send(command, ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{reportId:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Resolves a report, optionally suspending the property.")]
    [EndpointDescription("Resolves this report and every other pending report on the same property with the same optional note (up to 1000 characters, shown to the reporters). The body is optional. With suspendProperty=true an approved property is moved back to Rejected with suspensionReason (required, up to 500 characters) as the reason the owner sees; it disappears from search immediately and the owner can fix it and resubmit. When the property is already hidden (pending, rejected or deleted) the reports are resolved and nothing is suspended. Every reporter and, when suspended, the owner are notified. Returns 409 ReportErrors.AlreadyReviewed for a reviewed report or when another admin changed it at the same time.")]
    [EndpointName("ResolveReport")]
    public async Task<IActionResult> ResolveAsync([FromRoute] Guid reportId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ResolveReportCommand? request, CancellationToken ct)
    {
        var command = (request ?? new ResolveReportCommand(reportId, null, false, null)) with { ReportId = reportId };
        var result = await _sender.Send(command, ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
