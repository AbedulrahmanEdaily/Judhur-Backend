using System.Diagnostics;

using Judhur.Application.Common.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Common.Behaviors;
public sealed class PerformanceBehavior<TRequest, TResponse>(
    IUser user,
    ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int LongRunningThresholdMilliseconds = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            if (elapsed.TotalMilliseconds > LongRunningThresholdMilliseconds)
            {
                logger.LogWarning(
                    "Long running request: {RequestName} took {ElapsedMilliseconds} ms for {UserId} {@Request}",
                    typeof(TRequest).Name,
                    (long)elapsed.TotalMilliseconds,
                    user.Id,
                    request);
            }
        }
    }
}
