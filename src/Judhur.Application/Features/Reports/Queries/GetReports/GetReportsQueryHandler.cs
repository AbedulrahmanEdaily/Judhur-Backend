using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Reports.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Reports.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reports.Queries.GetReports;

public sealed class GetReportsQueryHandler(IAppDbContext context, ILogger<GetReportsQueryHandler> logger)
    : IRequestHandler<GetReportsQuery, Result<PaginatedList<ReportSummaryDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetReportsQueryHandler> _logger = logger;

    public async Task<Result<PaginatedList<ReportSummaryDto>>> Handle(GetReportsQuery request, CancellationToken cancellationToken)
    {
        if (!Pagination.IsPageValid(request.Page))
        {
            _logger.LogWarning("Get reports rejected: invalid page {Page}", request.Page);
            return ApplicationError.PageInvalid;
        }
        if (request.PageSize is <= 0 or > 100)
        {
            _logger.LogWarning("Get reports rejected: invalid page size {PageSize}", request.PageSize);
            return ApplicationError.PageSizeInvalid;
        }

        var query = _context.Reports
            .AsNoTracking()
            .Where(r => r.Status == request.Status);
        var count = await query.CountAsync(cancellationToken);
        var joined = query.Join(
            _context.Properties.IgnoreQueryFilters(),
            r => r.PropertyId,
            p => p.Id,
            (r, p) => new { Report = r, PropertyTitle = p.Title, PropertyIsDeleted = p.IsDeleted });
        var ordered = request.Status == ReportStatus.Pending
            ? joined.OrderBy(x => x.Report.CreatedAtUtc).ThenBy(x => x.Report.Id)
            : joined.OrderByDescending(x => x.Report.ReviewedAtUtc).ThenBy(x => x.Report.Id);
        var items = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ReportSummaryDto(
                x.Report.Id,
                x.Report.PropertyId,
                x.PropertyTitle,
                x.PropertyIsDeleted,
                x.Report.Reason,
                x.Report.Status,
                _context.Reports.Count(other => other.PropertyId == x.Report.PropertyId && other.Status == ReportStatus.Pending),
                x.Report.CreatedAtUtc,
                x.Report.ReviewedAtUtc))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} of {TotalCount} {Status} reports (page {Page}, size {PageSize})", items.Count, count, request.Status, request.Page, request.PageSize);
        return new PaginatedList<ReportSummaryDto>
        {
            Items = items,
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }
}
