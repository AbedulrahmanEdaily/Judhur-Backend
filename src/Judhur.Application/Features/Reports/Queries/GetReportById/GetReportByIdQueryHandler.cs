using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reports.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reports.Queries.GetReportById;

public sealed class GetReportByIdQueryHandler(IAppDbContext context, IIdentityService identityService, ILogger<GetReportByIdQueryHandler> logger)
    : IRequestHandler<GetReportByIdQuery, Result<ReportDetailsDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly IIdentityService _identityService = identityService;
    private readonly ILogger<GetReportByIdQueryHandler> _logger = logger;

    public async Task<Result<ReportDetailsDto>> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
    {
        var report = await _context.Reports
            .AsNoTracking()
            .Where(r => r.Id == request.ReportId)
            .Join(
                _context.Properties.IgnoreQueryFilters(),
                r => r.PropertyId,
                p => p.Id,
                (r, p) => new ReportDetailsDto
                {
                    Id = r.Id,
                    Reason = r.Reason,
                    Details = r.Details,
                    Status = r.Status,
                    AdminNote = r.AdminNote,
                    CreatedAtUtc = r.CreatedAtUtc,
                    ReviewedAtUtc = r.ReviewedAtUtc,
                    ReporterId = r.ReporterId,
                    Property = new ReportedPropertyDto(
                        p.Id,
                        p.Title,
                        p.City,
                        p.ModerationStatus,
                        p.IsActive,
                        p.IsDeleted,
                        p.PropertyImages
                            .Where(i => i.IsMainImage)
                            .Select(i => i.FileUrl)
                            .FirstOrDefault(),
                        p.SellerId),
                    OtherPendingReportsOnProperty = _context.Reports
                        .Count(other => other.PropertyId == r.PropertyId
                            && other.Id != r.Id
                            && other.Status == ReportStatus.Pending)
                })
            .FirstOrDefaultAsync(cancellationToken);
        if (report is null)
        {
            _logger.LogWarning("Get report rejected: report {ReportId} not found", request.ReportId);
            return ReportErrors.NotFound;
        }

        var reporterResult = await _identityService.GetUserInfoAsync(report.ReporterId.ToString(), cancellationToken);
        if (reporterResult.IsError)
        {
            _logger.LogWarning("Failed to load reporter {ReporterId} for report {ReportId}: {ErrorCode}", report.ReporterId, report.Id, reporterResult.TopError.Code);
        }
        else
        {
            report.Reporter = reporterResult.Value;
        }

        _logger.LogInformation("Report {ReportId} retrieved for review", report.Id);
        return report;
    }
}
