using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reports.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reports.Commands.CreateReport;

public sealed class CreateReportCommandHandler(IUser user, IAppDbContext context, ILogger<CreateReportCommandHandler> logger)
    : IRequestHandler<CreateReportCommand, Result<ReportCreatedDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<CreateReportCommandHandler> _logger = logger;

    public async Task<Result<ReportCreatedDto>> Handle(CreateReportCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } reporterId)
        {
            _logger.LogWarning("Create report rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var sellerId = await _context.Properties
            .AsNoTracking()
            .Where(p => p.Id == request.PropertyId
                && p.ModerationStatus == ModerationStatus.Approved
                && p.IsActive)
            .Select(p => (Guid?)p.SellerId)
            .FirstOrDefaultAsync(cancellationToken);
        if (sellerId is null)
        {
            _logger.LogWarning("Create report rejected: property {PropertyId} not found or not publicly visible", request.PropertyId);
            return PropertyErrors.NotFound;
        }

        var hasPendingReport = await _context.Reports
            .AnyAsync(r => r.ReporterId == reporterId
                && r.PropertyId == request.PropertyId
                && r.Status == ReportStatus.Pending,
                cancellationToken);
        if (hasPendingReport)
        {
            _logger.LogWarning("Create report rejected: user {UserId} already has a pending report on property {PropertyId}", reporterId, request.PropertyId);
            return ReportErrors.DuplicateReport;
        }

        var reportResult = Report.Create(Guid.CreateVersion7(), reporterId, request.PropertyId, sellerId.Value, request.Reason, request.Details);
        if (reportResult.IsError)
        {
            _logger.LogWarning("Create report rejected for user {UserId} on property {PropertyId}: {ErrorCode}", reporterId, request.PropertyId, reportResult.TopError.Code);
            return reportResult.Errors;
        }

        var report = reportResult.Value;
        _context.Reports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Report {ReportId} created by user {UserId} on property {PropertyId} with reason {Reason}", report.Id, reporterId, report.PropertyId, report.Reason);
        return new ReportCreatedDto(report.Id);
    }
}
