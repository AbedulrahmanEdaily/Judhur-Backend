using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Reports.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reports.Queries.GetMyReports;

public sealed class GetMyReportsQueryHandler(IUser user, IAppDbContext context, ILogger<GetMyReportsQueryHandler> logger)
    : IRequestHandler<GetMyReportsQuery, Result<PaginatedList<MyReportDto>>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetMyReportsQueryHandler> _logger = logger;

    public async Task<Result<PaginatedList<MyReportDto>>> Handle(GetMyReportsQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get my reports rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        if (request.Page <= 0)
        {
            _logger.LogWarning("Get my reports rejected: invalid page {Page}", request.Page);
            return ApplicationError.PageInvalid;
        }
        if (request.PageSize is <= 0 or > 100)
        {
            _logger.LogWarning("Get my reports rejected: invalid page size {PageSize}", request.PageSize);
            return ApplicationError.PageSizeInvalid;
        }

        var query = _context.Reports
            .AsNoTracking()
            .Where(r => r.ReporterId == userId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Join(
                _context.Properties.IgnoreQueryFilters(),
                r => r.PropertyId,
                p => p.Id,
                (r, p) => new { Report = r, PropertyTitle = p.Title })
            .OrderByDescending(x => x.Report.CreatedAtUtc)
            .ThenBy(x => x.Report.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new MyReportDto(
                x.Report.Id,
                x.Report.PropertyId,
                x.PropertyTitle,
                x.Report.Reason,
                x.Report.Details,
                x.Report.Status,
                x.Report.AdminNote,
                x.Report.CreatedAtUtc,
                x.Report.ReviewedAtUtc))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} of {TotalCount} reports for user {UserId} (page {Page}, size {PageSize})", items.Count, count, userId, request.Page, request.PageSize);
        return new PaginatedList<MyReportDto>
        {
            Items = items,
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }
}
