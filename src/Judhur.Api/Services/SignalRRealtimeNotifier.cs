using Judhur.Api.Hubs;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Conversations.Dto;
using Judhur.Application.Features.Notifications.Dto;

using Microsoft.AspNetCore.SignalR;

namespace Judhur.Api.Services;

public sealed class SignalRRealtimeNotifier(IHubContext<ChatHub> hubContext, ILogger<SignalRRealtimeNotifier> logger) : IRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext = hubContext;
    private readonly ILogger<SignalRRealtimeNotifier> _logger = logger;

    public Task MessageReceivedAsync(Guid recipientId, MessageDto message, CancellationToken cancellationToken = default)
        => SendAsync(recipientId, RealtimeEvents.MessageReceived, message, cancellationToken);

    public Task MessagesReadAsync(Guid recipientId, Guid conversationId, DateTimeOffset readAtUtc, CancellationToken cancellationToken = default)
        => SendAsync(recipientId, RealtimeEvents.MessagesRead, new { conversationId, readAtUtc }, cancellationToken);

    public Task NotificationReceivedAsync(Guid recipientId, NotificationDto notification, CancellationToken cancellationToken = default)
        => SendAsync(recipientId, RealtimeEvents.NotificationReceived, notification, cancellationToken);

    private async Task SendAsync(Guid recipientId, string eventName, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients.User(recipientId.ToString()).SendAsync(eventName, payload, cancellationToken);
            _logger.LogDebug("Realtime event {EventName} sent to user {UserId}", eventName, recipientId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Realtime event {EventName} could not be sent to user {UserId}", eventName, recipientId);
        }
    }
}
