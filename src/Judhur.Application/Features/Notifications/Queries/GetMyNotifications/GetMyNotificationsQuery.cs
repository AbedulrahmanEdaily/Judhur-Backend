using Judhur.Application.Common.Models;
using Judhur.Application.Features.Notifications.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(int Page, int PageSize) : IRequest<Result<PaginatedList<NotificationDto>>>;
