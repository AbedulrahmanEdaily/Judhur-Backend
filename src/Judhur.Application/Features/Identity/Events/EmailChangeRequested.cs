using MediatR;

namespace Judhur.Application.Features.Identity.Events;

public sealed record EmailChangeRequested(Guid UserId, string NewEmail) : INotification;
