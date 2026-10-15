using MediatR;

namespace Judhur.Application.Features.Users.Events;

public sealed record UserBanned(Guid UserId, string Reason, int DeactivatedListings) : INotification;
