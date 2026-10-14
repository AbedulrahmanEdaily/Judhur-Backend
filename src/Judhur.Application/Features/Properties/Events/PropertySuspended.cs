using MediatR;

namespace Judhur.Application.Features.Properties.Events;

public sealed record PropertySuspended(Guid PropertyId, Guid SellerId, string Title, string Reason) : INotification;
