using Microsoft.Extensions.Caching.Hybrid;

namespace Judhur.Application.UnitTests.TestDoubles;

public sealed record CacheWrite(string Key, string[] Tags, TimeSpan? Expiration);

public sealed class FakeHybridCache : HybridCache
{
    private readonly Dictionary<string, object?> _entries = [];

    public List<CacheWrite> Writes { get; } = [];

    public List<string> RemovedTags { get; } = [];

    public Exception? RemoveByTagException { get; set; }

    public bool Contains(string key) => _entries.ContainsKey(key);

    public void Seed<T>(string key, T value) => _entries[key] = value;

    public override async ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(key, out var cached))
        {
            return (T)cached!;
        }
        var value = await factory(state, cancellationToken);
        _entries[key] = value;
        Writes.Add(new CacheWrite(key, tags?.ToArray() ?? [], options?.Expiration));
        return value;
    }

    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        _entries[key] = value;
        Writes.Add(new CacheWrite(key, tags?.ToArray() ?? [], options?.Expiration));
        return ValueTask.CompletedTask;
    }

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _entries.Remove(key);
        return ValueTask.CompletedTask;
    }

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (RemoveByTagException is not null)
        {
            throw RemoveByTagException;
        }
        RemovedTags.Add(tag);
        return ValueTask.CompletedTask;
    }
}
