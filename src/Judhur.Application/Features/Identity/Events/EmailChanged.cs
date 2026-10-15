using MediatR;

namespace Judhur.Application.Features.Identity.Events;

public sealed record EmailChanged(Guid UserId, string OldEmail) : INotification;
