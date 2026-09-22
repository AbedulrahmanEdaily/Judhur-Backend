using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ResendConfirmation;

public sealed class ResendConfirmationCommandHandler(
    IIdentityService identityService,
    IPublisher publisher) : IRequestHandler<ResendConfirmationCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Success>> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
    {
        var userId = await _identityService.FindUnconfirmedUserIdAsync(request.Email, cancellationToken);
        if (userId is not null)
        {
            await _publisher.Publish(new UserRegistered(userId.Value), cancellationToken);
        }
        return Result.Success;
    }
}
