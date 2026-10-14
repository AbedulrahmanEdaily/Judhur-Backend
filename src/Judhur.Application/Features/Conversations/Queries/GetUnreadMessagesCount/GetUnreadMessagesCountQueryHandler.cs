using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Conversations.Queries.GetUnreadMessagesCount;

public sealed class GetUnreadMessagesCountQueryHandler(IUser user, IAppDbContext context, ILogger<GetUnreadMessagesCountQueryHandler> logger)
    : IRequestHandler<GetUnreadMessagesCountQuery, Result<UnreadMessagesCountDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetUnreadMessagesCountQueryHandler> _logger = logger;

    public async Task<Result<UnreadMessagesCountDto>> Handle(GetUnreadMessagesCountQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get unread messages count rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var count = await _context.Conversations
            .Where(c => c.BuyerId == userId || c.SellerId == userId)
            .SelectMany(c => c.Messages)
            .CountAsync(m => m.SenderId != userId && m.ReadAtUtc == null, cancellationToken);

        _logger.LogDebug("User {UserId} has {UnreadCount} unread messages", userId, count);
        return new UnreadMessagesCountDto(count);
    }
}
