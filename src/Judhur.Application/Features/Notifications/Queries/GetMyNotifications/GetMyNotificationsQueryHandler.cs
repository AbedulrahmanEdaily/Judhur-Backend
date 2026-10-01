using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Notifications.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler(IUser user, IAppDbContext context, ILogger<GetMyNotificationsQueryHandler> logger) : IRequestHandler<GetMyNotificationsQuery, Result<PaginatedList<NotificationDto>>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetMyNotificationsQueryHandler> _logger = logger;

    public async Task<Result<PaginatedList<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get my notifications rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        if (request.Page <= 0)
        {
            _logger.LogWarning("Get my notifications rejected: invalid page {Page}", request.Page);
            return ApplicationError.PageInvalid;
        }

        if (request.PageSize is <= 0 or > 100)
        {
            _logger.LogWarning("Get my notifications rejected: invalid page size {PageSize}", request.PageSize);
            return ApplicationError.PageSizeInvalid;
        }

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                ReferenceId = n.ReferenceId,
                IsRead = n.ReadAtUtc != null,
                CreatedAtUtc = n.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} of {TotalCount} notifications for user {UserId} (page {Page}, size {PageSize})", items.Count, count, userId, request.Page, request.PageSize);
        return new PaginatedList<NotificationDto>
        {
            Items = items,
            PageNumber = request.Page,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }
}
