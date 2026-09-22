using MediatR;

namespace Judhur.Application.Features.Identity.Events;

public sealed record PasswordChanged(Guid UserId) : INotification;