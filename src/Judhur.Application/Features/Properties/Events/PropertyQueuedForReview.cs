using MediatR;

namespace Judhur.Application.Features.Properties.Events;

public sealed record PropertyQueuedForReview(Guid PropertyId, string Title) : INotification;
