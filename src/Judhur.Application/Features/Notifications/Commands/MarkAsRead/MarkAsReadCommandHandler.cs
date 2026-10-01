using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Notifications;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.Commands.MarkAsRead;

public sealed class MarkAsReadCommandHandler(IUser user, IAppDbContext context, ILogger<MarkAsReadCommandHandler> logger, TimeProvider timeProvider) : IRequestHandler<MarkAsReadCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<MarkAsReadCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<Updated>> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Mark notification as read rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, cancellationToken);
        if (notification is null)
        {
            _logger.LogWarning("Mark notification as read rejected: notification {NotificationId} not found for user {UserId}", request.NotificationId, userId);
            return NotificationErrors.NotFound;
        }

        if (notification.IsRead)
        {
            _logger.LogDebug("Notification {NotificationId} was already read by user {UserId}", notification.Id, userId);
            return Result.Updated;
        }

        notification.MarkAsRead(_timeProvider.GetUtcNow());
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}", notification.Id, userId);
        return Result.Updated;
    }
}