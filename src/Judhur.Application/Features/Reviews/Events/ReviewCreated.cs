using MediatR;

namespace Judhur.Application.Features.Reviews.Events;

public sealed record ReviewCreated(Guid ReviewId, Guid SellerId, decimal Rating) : INotification;
