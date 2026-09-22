using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IIdentityService identityService,
    IDeferredDispatcher dispatcher,
    IPublisher publisher) : IRequestHandler<ChangePasswordCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Success>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ChangePasswordAsync(request.Email, request.Password, request.Code, cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }
        _dispatcher.Defer(ct => _publisher.Publish(new PasswordChanged(result.Value), ct));
        return Result.Success;
    }
}
