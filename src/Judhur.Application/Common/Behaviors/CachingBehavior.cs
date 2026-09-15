using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly HybridCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    public CachingBehavior(HybridCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICachedQuery cachedQuery)
        {
            return await next(cancellationToken);
        }
        _logger.LogInformation("Checking cache for {RequestName}", typeof(TRequest).Name);
        var result = await _cache.GetOrCreateAsync<TResponse>(cachedQuery.CacheKey, _ => new ValueTask<TResponse>((TResponse)(object)null!), new HybridCacheEntryOptions
        {
            Flags = HybridCacheEntryFlags.DisableUnderlyingData
        }, cancellationToken: cancellationToken);
        if (result is null)
        {
            result = await next(cancellationToken);
            if (result is IResult rs && rs.IsSuccess)
            {
                _logger.LogInformation("Caching result for {RequestName}", typeof(TRequest).Name);
                await _cache.SetAsync(cachedQuery.CacheKey, result, new HybridCacheEntryOptions
                {
                    Expiration = cachedQuery.Expiration
                }, cachedQuery.Tags, cancellationToken);
            }
        }
        return result;
    }
}