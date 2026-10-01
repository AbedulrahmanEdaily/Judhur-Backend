using MediatR;

namespace Judhur.Application.Features.Properties.Events;

public sealed record PropertyApproved(Guid PropertyId, Guid SellerId, string Title) : INotification;