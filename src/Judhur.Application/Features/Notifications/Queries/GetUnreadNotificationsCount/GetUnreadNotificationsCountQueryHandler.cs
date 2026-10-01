using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Notifications.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.Queries.GetUnreadNotificationsCount;

public sealed class GetUnreadNotificationsCountQueryHandler(IUser user, IAppDbContext context, ILogger<GetUnreadNotificationsCountQueryHandler> logger) : IRequestHandler<GetUnreadNotificationsCountQuery, Result<UnreadCountDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetUnreadNotificationsCountQueryHandler> _logger = logger;

    public async Task<Result<UnreadCountDto>> Handle(GetUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get unread notifications count rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var unreadCount = await _context.Notifications
            .CountAsync(n => n.UserId == userId && n.ReadAtUtc == null, cancellationToken);

        _logger.LogDebug("User {UserId} has {UnreadCount} unread notifications", userId, unreadCount);
        return new UnreadCountDto(unreadCount);
    }
}