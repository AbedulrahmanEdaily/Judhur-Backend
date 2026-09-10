using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Judhur.Infrastructure.Data.Interceptors;

/// <summary>
/// Stamps the audit columns on every <see cref="AuditableEntity"/> touched by a
/// save, so handlers never have to remember to do it themselves.
/// </summary>
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

        // Read both once: one save is one moment, and every row it writes
        // carries the same timestamp and the same author.
        var utcNow = timeProvider.GetUtcNow();
        var userId = user.Id;

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
