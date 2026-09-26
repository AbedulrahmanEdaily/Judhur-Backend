using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.ResendConfirmation;

public sealed class ResendConfirmationCommandHandler(
    IIdentityService identityService,
    IPublisher publisher,
    ILogger<ResendConfirmationCommandHandler> logger) : IRequestHandler<ResendConfirmationCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<ResendConfirmationCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
    {
        var userId = await _identityService.FindUnconfirmedUserIdAsync(request.Email, cancellationToken);
        if (userId is null)
        {
            // The client still gets success so the endpoint can't be used to probe which emails exist.
            _logger.LogWarning("Resend confirmation skipped: no unconfirmed user for the given email");
            return Result.Success;
        }
        await _publisher.Publish(new UserRegistered(userId.Value), cancellationToken);
        _logger.LogInformation("Confirmation email re-sent for user {UserId}", userId.Value);
        return Result.Success;
    }
}
