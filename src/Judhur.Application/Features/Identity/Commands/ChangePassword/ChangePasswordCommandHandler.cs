using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IIdentityService identityService,
    IDeferredDispatcher dispatcher,
    IPublisher publisher,
    ILogger<ChangePasswordCommandHandler> logger) : IRequestHandler<ChangePasswordCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<ChangePasswordCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ChangePasswordAsync(request.Email, request.Password, request.Code, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Change password failed: {ErrorCode}", result.TopError.Code);
            return result.Errors;
        }
        _dispatcher.Defer(ct => _publisher.Publish(new PasswordChanged(result.Value), ct));
        _logger.LogInformation("Password changed for user {UserId}", result.Value);
        return Result.Success;
    }
}
