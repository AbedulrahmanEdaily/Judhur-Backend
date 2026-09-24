using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.SendResetPasswordCode;

public sealed class SendResetPasswordCodeCommandHandler(
    IIdentityService identityService,
    IDeferredDispatcher dispatcher,
    IPublisher publisher,
    ILogger<SendResetPasswordCodeCommandHandler> logger) : IRequestHandler<SendResetPasswordCodeCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<SendResetPasswordCodeCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(SendResetPasswordCodeCommand request, CancellationToken cancellationToken)
    {
        var userId = await _identityService.SendResetPasswordCodeAsync(request.Email, cancellationToken);
        if (userId is null)
        {
            // The client still gets success so the endpoint can't be used to probe which emails exist.
            _logger.LogWarning("Password reset code skipped: no eligible user for the given email");
            return Result.Success;
        }
        _dispatcher.Defer(ct => _publisher.Publish(new PasswordResetRequested(userId.Value), ct));
        _logger.LogInformation("Password reset code issued for user {UserId}", userId.Value);
        return Result.Success;
    }
}
