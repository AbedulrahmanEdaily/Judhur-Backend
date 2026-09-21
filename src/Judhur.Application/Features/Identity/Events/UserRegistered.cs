
using MediatR;

namespace Judhur.Application.Features.Identity.Events;

public sealed record UserRegistered(Guid UserId) : INotification;