using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Common.Behaviors;

public sealed class CacheInvalidationBehavior<TRequest, TResponse>(
    HybridCache cache,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IInvalidateCacheCommand
    where TResponse : IResult
{
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);
        if (!response.IsSuccess || request.Tags is not { Length: > 0 })
        {
            return response;
        }
        try
        {
            await cache.RemoveByTagAsync(request.Tags, CancellationToken.None);
            _logger.LogInformation(
                "Invalidated cache tags {Tags} after {RequestName}",
                request.Tags,
                typeof(TRequest).Name);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to invalidate cache tags {Tags} after {RequestName}. The data is committed; cached entries stay stale until they expire",
                request.Tags,
                typeof(TRequest).Name);
        }
        return response;
    }
}
