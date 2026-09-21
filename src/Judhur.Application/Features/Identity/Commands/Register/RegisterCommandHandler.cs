
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    IDeferredDispatcher dispatcher,
    IPublisher publisher) : IRequestHandler<RegisterCommand, Result<Success>>
{

    private readonly IIdentityService _identityService = identityService;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;

    public async Task<Result<Success>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var register = new NewUserRegistration(
            UserName: request.UserName,
            FullName: request.FullName,
            Email: request.Email,
            PhoneNumber: request.PhoneNumber,
            City: request.City,
            Bio: request.Bio,
            ProfileImageUrl: request.ProfileImageUrl,
            Password: request.Password);
        var userResult = await _identityService.CreateNewUserAsync(register, cancellationToken);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }
        var userId = userResult.Value;
        _dispatcher.Defer(ct=> _publisher.Publish(new UserRegistered(userId),ct));
        return Result.Success;
    }

}
