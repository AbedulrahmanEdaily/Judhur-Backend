using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.RequestEmailChange;

public sealed class RequestEmailChangeCommandHandler(
    IUser user,
    IIdentityService identityService,
    IDeferredDispatcher dispatcher,
    IPublisher publisher,
    ILogger<RequestEmailChangeCommandHandler> logger) : IRequestHandler<RequestEmailChangeCommand, Result<Success>>
{
    private readonly IUser _user = user;
    private readonly IIdentityService _identityService = identityService;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<RequestEmailChangeCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(RequestEmailChangeCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Request email change rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var newEmail = request.NewEmail.Trim();
        var result = await _identityService.RequestEmailChangeAsync(userId, newEmail, request.Password, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Request email change failed for user {UserId}: {ErrorCode}", userId, result.TopError.Code);
            return result.Errors;
        }

        var requested = new EmailChangeRequested(userId, newEmail);
        _dispatcher.Defer(ct => _publisher.Publish(requested, ct));
        _logger.LogInformation("Email change requested by user {UserId}", userId);
        return Result.Success;
    }
}
