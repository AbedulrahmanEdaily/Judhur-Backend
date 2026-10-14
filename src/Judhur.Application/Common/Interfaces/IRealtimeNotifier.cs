using Judhur.Application.Features.Conversations.Dto;
using Judhur.Application.Features.Notifications.Dto;

namespace Judhur.Application.Common.Interfaces;

public interface IRealtimeNotifier
{
    Task MessageReceivedAsync(Guid recipientId, MessageDto message, CancellationToken cancellationToken = default);

    Task MessagesReadAsync(Guid recipientId, Guid conversationId, DateTimeOffset readAtUtc, CancellationToken cancellationToken = default);

    Task NotificationReceivedAsync(Guid recipientId, NotificationDto notification, CancellationToken cancellationToken = default);
}
