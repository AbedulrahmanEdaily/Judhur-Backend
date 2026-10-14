using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Reports.Events;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Reports;
using Judhur.Domain.Reports.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Reports.Commands.DismissReport;

public sealed class DismissReportCommandHandler(
    IUser user,
    IAppDbContext context,
    ILogger<DismissReportCommandHandler> logger,
    TimeProvider timeProvider,
    IDeferredDispatcher dispatcher,
    IPublisher publisher) : IRequestHandler<DismissReportCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<DismissReportCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Updated>> Handle(DismissReportCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } adminId)
        {
            _logger.LogWarning("Dismiss report rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);
        if (report is null)
        {
            _logger.LogWarning("Dismiss report rejected: report {ReportId} not found", request.ReportId);
            return ReportErrors.NotFound;
        }

        var dismissResult = report.Dismiss(adminId, _timeProvider.GetUtcNow(), request.AdminNote);
        if (dismissResult.IsError)
        {
            _logger.LogWarning("Dismiss report {ReportId} failed: {ErrorCode}", report.Id, dismissResult.TopError.Code);
            return dismissResult.Errors;
        }

        var propertyTitle = await _context.Properties
            .IgnoreQueryFilters()
            .Where(p => p.Id == report.PropertyId)
            .Select(p => p.Title)
            .FirstAsync(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        _dispatcher.Defer(ct => _publisher.Publish(
            new ReportReviewed(report.Id, report.ReporterId, report.PropertyId, propertyTitle, ReportStatus.Dismissed, report.AdminNote), ct));
        _logger.LogInformation("Report {ReportId} on property {PropertyId} dismissed by admin {AdminId}", report.Id, report.PropertyId, adminId);
        return Result.Updated;
    }
}
