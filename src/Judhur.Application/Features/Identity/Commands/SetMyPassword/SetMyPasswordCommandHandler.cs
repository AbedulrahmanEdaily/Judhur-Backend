using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.SetMyPassword;

public sealed class SetMyPasswordCommandHandler(
    IUser user,
    IIdentityService identityService,
    IDeferredDispatcher dispatcher,
    IPublisher publisher,
    ILogger<SetMyPasswordCommandHandler> logger) : IRequestHandler<SetMyPasswordCommand, Result<Success>>
{
    private readonly IUser _user = user;
    private readonly IIdentityService _identityService = identityService;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<SetMyPasswordCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(SetMyPasswordCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Set my password rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var result = await _identityService.SetPasswordAsync(userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Set my password failed for user {UserId}: {ErrorCode}", userId, result.TopError.Code);
            return result.Errors;
        }

        _dispatcher.Defer(ct => _publisher.Publish(new PasswordChanged(userId), ct));
        _logger.LogInformation("Password set for user {UserId}", userId);
        return Result.Success;
    }
}
