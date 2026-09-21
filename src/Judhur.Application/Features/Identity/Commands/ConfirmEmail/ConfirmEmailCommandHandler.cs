using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(IIdentityService identityService) : IRequestHandler<ConfirmEmailCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<Result<Success>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        return _identityService.ConfirmEmailAsync(request.UserId, request.Token, cancellationToken);
    }
}