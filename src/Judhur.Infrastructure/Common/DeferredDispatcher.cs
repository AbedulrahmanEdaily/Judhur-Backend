using Judhur.Application.Common.Interfaces;

using Microsoft.Extensions.Logging;
namespace Judhur.Infrastructure.Common;

internal sealed class DeferredDispatcher(ILogger<DeferredDispatcher> logger) : IDeferredDispatcher
{
    private readonly ILogger<DeferredDispatcher> _logger = logger;
    private readonly List<Func<CancellationToken, Task>> _work = [];
    public void Defer(Func<CancellationToken, Task> work) => _work.Add(work);

    public async Task DispatchAsync(CancellationToken ct = default)
    {
        while(_work.Count > 0)
        {
            var pending = _work.ToArray();
            _work.Clear();
            foreach(var work in pending)
            {
                try
                {
                    await work(ct);
                }
                catch (Exception ex)
                {
                    
                    _logger.LogError(ex,"Deferred work failed after the transaction committed.");
                }
            }
        }
    }
}