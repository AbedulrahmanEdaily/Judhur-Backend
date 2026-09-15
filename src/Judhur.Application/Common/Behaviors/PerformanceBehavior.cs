using System.Diagnostics;

using Judhur.Application.Common.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger;
    private readonly IIdentityService _identityService;
    private readonly IUser _user;
    private readonly Stopwatch _timer;

    public PerformanceBehavior(ILogger<TRequest> logger, IIdentityService identityService, IUser user)
    {
        _logger = logger;
        _timer = new Stopwatch();
        _identityService = identityService;
        _user = user;
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = default(TResponse);
        try
        {
            _timer.Start();
            response = await next(cancellationToken);
        }
        finally
        {
            _timer.Stop();
            var elapsedMilliseconds = _timer.ElapsedMilliseconds;
            if (elapsedMilliseconds > 500)
            {
                var userId = _user.Id ?? Guid.Empty;
                var requestName = typeof(TRequest).Name;
                _logger.LogWarning(
                    "Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@UserId} {@Request}",
                    requestName,
                    elapsedMilliseconds,
                    userId,
                    request);
            }
        }
        return response;
    }
}