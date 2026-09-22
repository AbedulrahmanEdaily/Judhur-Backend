using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.SendResetPasswordCode;

public sealed class SendResetPasswordCodeCommandHandler(
    IIdentityService identityService,
    IDeferredDispatcher dispatcher,
    IPublisher publisher) : IRequestHandler<SendResetPasswordCodeCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Success>> Handle(SendResetPasswordCodeCommand request, CancellationToken cancellationToken)
    {
        var userId = await _identityService.SendResetPasswordCodeAsync(request.Email, cancellationToken);
        if (userId is not null)
        {
            _dispatcher.Defer(ct => _publisher.Publish(new PasswordResetRequested(userId.Value), ct));
        }
        return Result.Success;
    }
}
