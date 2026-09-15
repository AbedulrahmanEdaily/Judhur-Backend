using Judhur.Application.Common.Interfaces;

using MediatR.Pipeline;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest>(IUser user, ILogger<TRequest> logger)
    : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    private readonly IUser _user = user;

    private readonly ILogger<TRequest> _logger = logger;


    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Request: {RequestName} by {UserId} {@Request}",
            typeof(TRequest).Name,
            _user.Id,
            request);

        return Task.CompletedTask;
    }
}
