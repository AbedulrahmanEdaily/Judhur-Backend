namespace Judhur.Application.Common.Interfaces;

public interface IDeferredDispatcher
{
    void Defer(Func<CancellationToken, Task> work);
    Task DispatchAsync(CancellationToken ct = default);
}