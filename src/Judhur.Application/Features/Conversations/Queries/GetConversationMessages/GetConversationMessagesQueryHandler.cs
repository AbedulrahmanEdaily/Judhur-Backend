using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Conversations;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Conversations.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandler(IUser user, IAppDbContext context, ILogger<GetConversationMessagesQueryHandler> logger)
    : IRequestHandler<GetConversationMessagesQuery, Result<MessagesPageDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetConversationMessagesQueryHandler> _logger = logger;

    public async Task<Result<MessagesPageDto>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get conversation messages rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var isParticipant = await _context.Conversations
            .AnyAsync(c => c.Id == request.ConversationId && (c.BuyerId == userId || c.SellerId == userId), cancellationToken);
        if (!isParticipant)
        {
            _logger.LogWarning("Get conversation messages rejected: conversation {ConversationId} not found for user {UserId}", request.ConversationId, userId);
            return ConversationErrors.NotFound;
        }

        var query = _context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == request.ConversationId);
        if (request.Before is { } beforeId)
        {
            var cursor = await query
                .Where(m => m.Id == beforeId)
                .Select(m => new { m.SentAtUtc })
                .FirstOrDefaultAsync(cancellationToken);
            if (cursor is null)
            {
                _logger.LogWarning("Get conversation messages rejected: cursor {MessageId} is not in conversation {ConversationId}", beforeId, request.ConversationId);
                return ConversationErrors.InvalidCursor;
            }
            var cursorSentAtUtc = cursor.SentAtUtc;
            query = query.Where(m => m.SentAtUtc < cursorSentAtUtc
                || (m.SentAtUtc == cursorSentAtUtc && m.Id < beforeId));
        }

        var messages = await query
            .OrderByDescending(m => m.SentAtUtc)
            .ThenByDescending(m => m.Id)
            .Take(request.Limit + 1)
            .Select(m => new MessageDto(
                m.Id,
                m.ConversationId,
                m.SenderId,
                m.Content,
                m.SentAtUtc,
                m.ReadAtUtc,
                m.SenderId == userId))
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > request.Limit;
        if (hasMore)
        {
            messages.RemoveAt(messages.Count - 1);
        }

        _logger.LogInformation("Retrieved {Count} messages from conversation {ConversationId} for user {UserId}", messages.Count, request.ConversationId, userId);
        return new MessagesPageDto(messages, hasMore);
    }
}
