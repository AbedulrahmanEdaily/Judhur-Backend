using MediatR;

namespace Judhur.Application.Common.Interfaces;

/// <summary>
/// A command whose success makes cached data stale. The tags it declares are the ones
/// cleared once the command has committed. Keep them coarse — one per aggregate rather
/// than one per query — so adding a cached query does not mean revisiting every command.
/// </summary>
public interface IInvalidateCacheCommand
{
    string[] Tags { get; }
}

public interface IInvalidateCacheCommand<TResponse> : IRequest<TResponse>, IInvalidateCacheCommand;
