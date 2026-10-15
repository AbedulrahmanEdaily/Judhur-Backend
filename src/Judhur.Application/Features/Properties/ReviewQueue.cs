using System.Linq.Expressions;

using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Events;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Judhur.Application.Features.Properties;

public sealed class ReviewQueue(IAppDbContext context, IDeferredDispatcher dispatcher, IPublisher publisher)
{
    public static readonly Expression<Func<Property, bool>> IsReady = p =>
        p.ModerationStatus == ModerationStatus.Pending
        && p.IsActive
        && p.OwnershipDocumentPublicId != null
        && p.PropertyImages.Count >= Property.MinImages
        && p.PropertyImages.Any(i => i.IsMainImage);

    private readonly IAppDbContext _context = context;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public Task<bool> ContainsAsync(Guid propertyId, CancellationToken cancellationToken)
        => _context.Properties
            .Where(p => p.Id == propertyId)
            .Where(IsReady)
            .AnyAsync(cancellationToken);

    public async Task<bool> PublishIfEnteredAsync(Guid propertyId, string title, bool wasQueued, CancellationToken cancellationToken)
    {
        if (wasQueued || !await ContainsAsync(propertyId, cancellationToken))
        {
            return false;
        }

        var queued = new PropertyQueuedForReview(propertyId, title);
        _dispatcher.Defer(ct => _publisher.Publish(queued, ct));
        return true;
    }
}
