using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Conversations.Events;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Conversations;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Conversations.Commands.MarkConversationAsRead;

public sealed class MarkConversationAsReadCommandHandler(
    IUser user,
    IAppDbContext context,
    ILogger<MarkConversationAsReadCommandHandler> logger,
    TimeProvider timeProvider,
    IDeferredDispatcher dispatcher,
    IPublisher publisher) : IRequestHandler<MarkConversationAsReadCommand, Result<Updated>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<MarkConversationAsReadCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Updated>> Handle(MarkConversationAsReadCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } readerId)
        {
            _logger.LogWarning("Mark conversation as read rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var conversation = await _context.Conversations
            .Include(c => c.Messages.Where(m => m.SenderId != readerId && m.ReadAtUtc == null))
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId
                && (c.BuyerId == readerId || c.SellerId == readerId),
                cancellationToken);
        if (conversation is null)
        {
            _logger.LogWarning("Mark conversation as read rejected: conversation {ConversationId} not found for user {UserId}", request.ConversationId, readerId);
            return ConversationErrors.NotFound;
        }

        var unreadCount = conversation.Messages.Count;
        if (unreadCount == 0)
        {
            _logger.LogDebug("Conversation {ConversationId} has no unread messages for user {UserId}", conversation.Id, readerId);
            return Result.Updated;
        }

        var readAtUtc = _timeProvider.GetUtcNow();
        var markResult = conversation.MarkAsRead(readerId, readAtUtc);
        if (markResult.IsError)
        {
            _logger.LogWarning("Mark conversation {ConversationId} as read failed: {ErrorCode}", conversation.Id, markResult.TopError.Code);
            return markResult.Errors;
        }
        await _context.SaveChangesAsync(cancellationToken);

        var read = new MessagesRead(conversation.Id, readerId, conversation.OtherParticipantOf(readerId), readAtUtc);
        _dispatcher.Defer(ct => _publisher.Publish(read, ct));
        _logger.LogInformation("Marked {Count} messages as read in conversation {ConversationId} for user {UserId}", unreadCount, conversation.Id, readerId);
        return Result.Updated;
    }
}
