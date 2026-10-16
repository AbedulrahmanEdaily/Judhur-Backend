using Judhur.Application.Common.Interfaces;

namespace Judhur.Application.UnitTests.TestDoubles;

public sealed class RecordingDeferredDispatcher : IDeferredDispatcher
{
    private readonly List<Func<CancellationToken, Task>> _work = [];

    public int DeferredCount => _work.Count;

    public void Defer(Func<CancellationToken, Task> work) => _work.Add(work);

    public async Task DispatchAsync(CancellationToken ct = default)
    {
        var pending = _work.ToList();
        _work.Clear();
        foreach (var work in pending)
        {
            await work(ct);
        }
    }
}
