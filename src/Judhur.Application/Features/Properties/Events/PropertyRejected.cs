using MediatR;

namespace Judhur.Application.Features.Properties.Events;

public sealed record PropertyRejected(Guid PropertyId, Guid SellerId, string Title, string Reason) : INotification;