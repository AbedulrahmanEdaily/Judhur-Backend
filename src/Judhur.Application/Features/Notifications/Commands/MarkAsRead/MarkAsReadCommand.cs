using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Notifications.Commands.MarkAsRead;

public sealed record MarkAsReadCommand(Guid NotificationId) : IRequest<Result<Updated>>;