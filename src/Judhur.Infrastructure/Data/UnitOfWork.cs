using Judhur.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Judhur.Infrastructure.Data;

internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly AppDbContext _context = context;

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new EfTransactionScope(await _context.Database.BeginTransactionAsync(cancellationToken));
}

internal sealed class EfTransactionScope(IDbContextTransaction transaction) : ITransactionScope
{
    private readonly IDbContextTransaction _transaction = transaction;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
