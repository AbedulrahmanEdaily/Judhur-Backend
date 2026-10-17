using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common;
using Judhur.Domain.Properties;
using Judhur.Domain.Properties.PropertyImages;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Judhur.Infrastructure.Data.Interceptors;

public sealed class AuditableEntityInterceptor(IUser user, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = timeProvider.GetUtcNow();
        var userId = user.Id;

        var propertiesWithChangedImages = context.ChangeTracker.Entries<PropertyImage>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.PropertyId)
            .ToHashSet();
        foreach (var entry in context.ChangeTracker.Entries<Property>())
        {
            if (entry.State is EntityState.Unchanged && propertiesWithChangedImages.Contains(entry.Entity.Id))
            {
                entry.State = EntityState.Modified;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State is EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.LastModifiedUtc = utcNow;
                entry.Entity.LastModifiedBy = userId;
            }
        }
    }
}
