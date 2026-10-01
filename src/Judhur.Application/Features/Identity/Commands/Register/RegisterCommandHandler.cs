
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    IDeferredDispatcher dispatcher,
    IPublisher publisher,
    ILogger<RegisterCommandHandler> logger) : IRequestHandler<RegisterCommand, Result<Success>>
{

    private readonly IIdentityService _identityService = identityService;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<RegisterCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var register = new NewUserRegistration(
            FullName: request.FullName,
            Email: request.Email,
            PhoneNumber: request.PhoneNumber,
            City: request.City,
            Bio: request.Bio,
            Password: request.Password);
        var userResult = await _identityService.CreateNewUserAsync(register, cancellationToken);
        if (userResult.IsError)
        {
            _logger.LogWarning("Registration failed: {ErrorCode}", userResult.TopError.Code);
            return userResult.Errors;
        }
        var userId = userResult.Value;
        _dispatcher.Defer(ct => _publisher.Publish(new UserRegistered(userId), ct));
        _logger.LogInformation("User {UserId} registered", userId);
        return Result.Success;
    }

}
