using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ResendConfirmation;

public sealed class ResendConfirmationCommandHandler(
    IIdentityService identityService,
    IEmailQueue emailQueue) : IRequestHandler<ResendConfirmationCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailQueue _emailQueue = emailQueue;

    public async Task<Result<Success>> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
    {
        var userId = await _identityService.FindUnconfirmedUserIdAsync(request.Email, cancellationToken);

        if (userId is not null)
        {
            await _emailQueue.EnqueueAsync(new EmailConfirmationRequest(userId.Value), cancellationToken);
        }

        return Result.Success;
    }
}
