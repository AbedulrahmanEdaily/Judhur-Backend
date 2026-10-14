using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Application.Features.Conversations.Events;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Conversations;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Conversations.Commands.StartConversation;

public sealed class StartConversationCommandHandler(
    IUser user,
    IAppDbContext context,
    ILogger<StartConversationCommandHandler> logger,
    TimeProvider timeProvider,
    IDeferredDispatcher dispatcher,
    IPublisher publisher) : IRequestHandler<StartConversationCommand, Result<StartConversationResultDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<StartConversationCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<StartConversationResultDto>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } buyerId)
        {
            _logger.LogWarning("Start conversation rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var sellerId = await _context.Properties
            .AsNoTracking()
            .Where(p => p.Id == request.PropertyId
                && p.ModerationStatus == ModerationStatus.Approved
                && p.IsActive)
            .Select(p => (Guid?)p.SellerId)
            .FirstOrDefaultAsync(cancellationToken);
        if (sellerId is null)
        {
            _logger.LogWarning("Start conversation rejected: property {PropertyId} not found or not publicly visible", request.PropertyId);
            return PropertyErrors.NotFound;
        }

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId && c.PropertyId == request.PropertyId, cancellationToken);
        var isNew = conversation is null;
        if (conversation is null)
        {
            var conversationResult = Conversation.Create(Guid.CreateVersion7(), buyerId, sellerId.Value, request.PropertyId);
            if (conversationResult.IsError)
            {
                _logger.LogWarning("Start conversation rejected for user {UserId} on property {PropertyId}: {ErrorCode}", buyerId, request.PropertyId, conversationResult.TopError.Code);
                return conversationResult.Errors;
            }
            conversation = conversationResult.Value;
            _context.Conversations.Add(conversation);
        }

        var messageResult = conversation.AddMessage(Guid.CreateVersion7(), buyerId, request.Message, _timeProvider.GetUtcNow());
        if (messageResult.IsError)
        {
            _logger.LogWarning("Start conversation rejected for user {UserId} on property {PropertyId}: {ErrorCode}", buyerId, request.PropertyId, messageResult.TopError.Code);
            return messageResult.Errors;
        }
        var message = messageResult.Value;
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        var sent = new MessageSent(message.Id, conversation.Id, buyerId, conversation.SellerId, message.Content, message.SentAtUtc);
        _dispatcher.Defer(ct => _publisher.Publish(sent, ct));
        _logger.LogInformation(
            "Message {MessageId} sent by buyer {UserId} on property {PropertyId} in {State} conversation {ConversationId}",
            message.Id,
            buyerId,
            request.PropertyId,
            isNew ? "new" : "existing",
            conversation.Id);
        return new StartConversationResultDto(
            conversation.Id,
            isNew,
            new MessageDto(message.Id, conversation.Id, buyerId, message.Content, message.SentAtUtc, null, true));
    }
}
