using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.Commands.MarkAllAsRead;

public sealed class MarkAllAsReadCommandHandler(IUser user, IAppDbContext context, ILogger<MarkAllAsReadCommandHandler> logger, TimeProvider timeProvider) : IRequestHandler<MarkAllAsReadCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<MarkAllAsReadCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<Updated>> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Mark all notifications as read rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && n.ReadAtUtc == null)
            .ToListAsync(cancellationToken);
        if (unread.Count == 0)
        {
            _logger.LogDebug("User {UserId} has no unread notifications to mark as read", userId);
            return Result.Updated;
        }

        var now = _timeProvider.GetUtcNow();
        foreach (var notification in unread)
        {
            notification.MarkAsRead(now);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unread.Count, userId);
        return Result.Updated;
    }
}