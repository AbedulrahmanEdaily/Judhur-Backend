using Judhur.Application.Common.Interfaces;

using MediatR.Pipeline;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Common.Behaviors;

public class LoggingBehavior<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    private readonly IUser _user;
    private readonly ILogger<TRequest> _logger;
    private readonly IIdentityService _identityService;

    public LoggingBehavior(IUser user, ILogger<TRequest> logger, IIdentityService identityService)
    {
        _user = user;
        _logger = logger;
        _identityService = identityService;
    }
    public async Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _user.Id ?? Guid.Empty;
        string? userName = string.Empty;
        if (userId != Guid.Empty)
        {
            userName = await _identityService.GetUserNameAsync(userId);
        }
        _logger.LogInformation("Request: {Name} {@UserId} {@UserName} {@Request}", requestName, userId, userName, request);
    }
}