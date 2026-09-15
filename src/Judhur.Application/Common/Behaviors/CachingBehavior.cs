using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Common.Behaviors;

/// <summary>
/// Serves cacheable queries from the cache, and runs the handler only on a miss.
///
/// The constraint on <typeparamref name="TRequest"/> is what keeps this out of the
/// pipeline entirely for every other request: the container checks generic constraints
/// and skips behaviours that cannot close, so there is no per-request type test here.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(
    HybridCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : IResult
{
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger = logger;


    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await cache.GetOrCreateAsync<TResponse>(
                request.CacheKey,
                async ct =>
                {
                    _logger.LogInformation(
                        "Cache miss for {RequestName}, running the handler",
                        typeof(TRequest).Name);
                    var result = await next(ct);
                    if (!result.IsSuccess)
                    {
                        throw new UncacheableResultException(result);
                    }
                    return result;
                },
                new HybridCacheEntryOptions { Expiration = request.Expiration },
                request.Tags,
                cancellationToken);
        }
        catch (UncacheableResultException uncacheable)
        {
            return uncacheable.Response;
        }
    }
    private sealed class UncacheableResultException(TResponse response) : Exception
    {
        public TResponse Response { get; } = response;
    }
}
