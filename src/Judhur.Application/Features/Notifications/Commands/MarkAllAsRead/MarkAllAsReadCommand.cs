using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Notifications.Commands.MarkAllAsRead;

public sealed record MarkAllAsReadCommand : IRequest<Result<Updated>>;
