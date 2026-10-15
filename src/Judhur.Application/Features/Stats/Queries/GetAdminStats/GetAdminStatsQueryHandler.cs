using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties;
using Judhur.Application.Features.Stats.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;
using Judhur.Domain.Reports.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Stats.Queries.GetAdminStats;

public sealed class GetAdminStatsQueryHandler(IAppDbContext context, ILogger<GetAdminStatsQueryHandler> logger, IIdentityService identityService, TimeProvider timeProvider)
    : IRequestHandler<GetAdminStatsQuery, Result<AdminStatsDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetAdminStatsQueryHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<AdminStatsDto>> Handle(GetAdminStatsQuery request, CancellationToken cancellationToken)
    {
        var sinceUtc = _timeProvider.GetUtcNow().AddDays(-7);

        var users = await _identityService.GetUserStatsAsync(sinceUtc, cancellationToken);

        var properties = _context.Properties.AsNoTracking();
        var published = await properties.CountAsync(p => p.ModerationStatus == ModerationStatus.Approved && p.IsActive, cancellationToken);
        var pending = await properties.CountAsync(p => p.ModerationStatus == ModerationStatus.Pending && p.IsActive, cancellationToken);
        var waitingForReview = await properties.CountAsync(ReviewQueue.IsReady, cancellationToken);
        var rejected = await properties.CountAsync(p => p.ModerationStatus == ModerationStatus.Rejected, cancellationToken);
        var inactive = await properties.CountAsync(p => !p.IsActive, cancellationToken);
        var sold = await properties.CountAsync(p => p.PropertyStatus == PropertyStatus.Sold, cancellationToken);
        var rented = await properties.CountAsync(p => p.PropertyStatus == PropertyStatus.Rented, cancellationToken);
        var newListings = await properties.CountAsync(p => p.CreatedAtUtc >= sinceUtc, cancellationToken);

        var pendingReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending, cancellationToken);
        var newReports = await _context.Reports.CountAsync(r => r.CreatedAtUtc >= sinceUtc, cancellationToken);
        var messages = await _context.Messages.CountAsync(m => m.SentAtUtc >= sinceUtc, cancellationToken);
        var reviews = await _context.Reviews.CountAsync(r => r.CreatedAtUtc >= sinceUtc, cancellationToken);

        var stats = new AdminStatsDto(
            users,
            new PropertyStatsDto(published, waitingForReview, pending - waitingForReview, rejected, inactive, sold, rented),
            pendingReports,
            new ActivityStatsDto(users.NewUsers, newListings, messages, reviews, newReports));

        _logger.LogInformation("Admin stats computed: {Published} published, {WaitingForReview} waiting for review, {PendingReports} pending reports", published, waitingForReview, pendingReports);
        return stats;
    }
}
