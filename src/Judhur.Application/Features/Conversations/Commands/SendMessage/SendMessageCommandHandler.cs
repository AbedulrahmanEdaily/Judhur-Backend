using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Application.Features.Conversations.Events;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Conversations;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Conversations.Commands.SendMessage;

public sealed class SendMessageCommandHandler(
    IUser user,
    IAppDbContext context,
    ILogger<SendMessageCommandHandler> logger,
    TimeProvider timeProvider,
    IDeferredDispatcher dispatcher,
    IPublisher publisher,
    IIdentityService identityService) : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<SendMessageCommandHandler> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;
    private readonly IIdentityService _identityService = identityService;

    public async Task<Result<MessageDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } senderId)
        {
            _logger.LogWarning("Send message rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId
                && (c.BuyerId == senderId || c.SellerId == senderId),
                cancellationToken);
        if (conversation is null)
        {
            _logger.LogWarning("Send message rejected: conversation {ConversationId} not found for user {UserId}", request.ConversationId, senderId);
            return ConversationErrors.NotFound;
        }

        var recipientId = conversation.OtherParticipantOf(senderId);
        if (!await _identityService.IsActiveAccountAsync(recipientId, cancellationToken))
        {
            _logger.LogWarning("Send message rejected: recipient {RecipientId} of conversation {ConversationId} is banned or deleted", recipientId, conversation.Id);
            return ApplicationError.ParticipantUnavailable;
        }

        var messageResult = conversation.AddMessage(Guid.CreateVersion7(), senderId, request.Content, _timeProvider.GetUtcNow());
        if (messageResult.IsError)
        {
            _logger.LogWarning("Send message in conversation {ConversationId} failed: {ErrorCode}", conversation.Id, messageResult.TopError.Code);
            return messageResult.Errors;
        }
        var message = messageResult.Value;
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        var sent = new MessageSent(message.Id, conversation.Id, senderId, recipientId, message.Content, message.SentAtUtc);
        _dispatcher.Defer(ct => _publisher.Publish(sent, ct));
        _logger.LogInformation("Message {MessageId} sent by user {UserId} in conversation {ConversationId}", message.Id, senderId, conversation.Id);
        return new MessageDto(message.Id, conversation.Id, senderId, message.Content, message.SentAtUtc, null, true);
    }
}
