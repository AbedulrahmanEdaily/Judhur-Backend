using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Events;
using Judhur.Application.Features.Reports.Events;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;
using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reports.Commands.ResolveReport;

public sealed class ResolveReportCommandHandler(
    IUser user,
    IAppDbContext context,
    ILogger<ResolveReportCommandHandler> logger,
    TimeProvider timeProvider,
    IDeferredDispatcher dispatcher,
    IPublisher publisher) : IRequestHandler<ResolveReportCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<ResolveReportCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Updated>> Handle(ResolveReportCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } adminId)
        {
            _logger.LogWarning("Resolve report rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);
        if (report is null)
        {
            _logger.LogWarning("Resolve report rejected: report {ReportId} not found", request.ReportId);
            return ReportErrors.NotFound;
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var resolveResult = report.Resolve(adminId, nowUtc, request.AdminNote);
        if (resolveResult.IsError)
        {
            _logger.LogWarning("Resolve report {ReportId} failed: {ErrorCode}", report.Id, resolveResult.TopError.Code);
            return resolveResult.Errors;
        }

        var otherPendingReports = await _context.Reports
            .Where(r => r.PropertyId == report.PropertyId
                && r.Id != report.Id
                && r.Status == ReportStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var otherReport in otherPendingReports)
        {
            var otherResult = otherReport.Resolve(adminId, nowUtc, request.AdminNote);
            if (otherResult.IsError)
            {
                _logger.LogWarning("Resolve report {ReportId} failed while resolving related report {RelatedReportId}: {ErrorCode}", report.Id, otherReport.Id, otherResult.TopError.Code);
                return otherResult.Errors;
            }
        }

        var property = await _context.Properties
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == report.PropertyId, cancellationToken);

        Property? suspendedProperty = null;
        if (request.SuspendProperty)
        {
            if (property.IsDeleted || property.ModerationStatus != ModerationStatus.Approved)
            {
                _logger.LogInformation(
                    "Report {ReportId}: property {PropertyId} is not published (deleted: {IsDeleted}, moderation: {ModerationStatus}), suspension skipped",
                    report.Id,
                    property.Id,
                    property.IsDeleted,
                    property.ModerationStatus);
            }
            else
            {
                var suspendResult = property.SuspendByAdmin(adminId, nowUtc, request.SuspensionReason!);
                if (suspendResult.IsError)
                {
                    _logger.LogWarning("Resolve report {ReportId} rejected: suspending property {PropertyId} failed: {ErrorCode}", report.Id, property.Id, suspendResult.TopError.Code);
                    return suspendResult.Errors;
                }
                suspendedProperty = property;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var resolved in otherPendingReports.Prepend(report))
        {
            var reviewed = new ReportReviewed(resolved.Id, resolved.ReporterId, resolved.PropertyId, property.Title, ReportStatus.Resolved, resolved.AdminNote);
            _dispatcher.Defer(ct => _publisher.Publish(reviewed, ct));
        }
        if (suspendedProperty is not null)
        {
            var suspended = new PropertySuspended(suspendedProperty.Id, suspendedProperty.SellerId, suspendedProperty.Title, suspendedProperty.RejectionReason!);
            _dispatcher.Defer(ct => _publisher.Publish(suspended, ct));
        }

        _logger.LogInformation(
            "Report {ReportId} resolved by admin {AdminId} with {RelatedCount} related reports on property {PropertyId}; property suspended: {Suspended}",
            report.Id,
            adminId,
            otherPendingReports.Count,
            report.PropertyId,
            suspendedProperty is not null);
        return Result.Updated;
    }
}
