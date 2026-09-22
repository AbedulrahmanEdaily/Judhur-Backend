using MediatR;

namespace Judhur.Application.Features.Identity.Events;

public sealed record PasswordResetRequested(Guid UserId) : INotification;