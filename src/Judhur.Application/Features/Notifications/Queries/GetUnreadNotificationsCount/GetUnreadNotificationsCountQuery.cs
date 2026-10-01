using Judhur.Application.Features.Notifications.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Notifications.Queries.GetUnreadNotificationsCount;

public sealed record GetUnreadNotificationsCountQuery : IRequest<Result<UnreadCountDto>>;
