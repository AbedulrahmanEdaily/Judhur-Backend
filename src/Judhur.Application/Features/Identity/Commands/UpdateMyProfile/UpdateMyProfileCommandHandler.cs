using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler(IUser user, IIdentityService identityService, ILogger<UpdateMyProfileCommandHandler> logger)
    : IRequestHandler<UpdateMyProfileCommand, Result<MyProfileDto>>
{
    private readonly IUser _user = user;
    private readonly IIdentityService _identityService = identityService;
    private readonly ILogger<UpdateMyProfileCommandHandler> _logger = logger;

    public async Task<Result<MyProfileDto>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Update my profile rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var update = new ProfileUpdate(request.FullName, request.PhoneNumber, request.City, request.Bio);
        var result = await _identityService.UpdateProfileAsync(userId, update, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Update my profile failed for user {UserId}: {ErrorCode}", userId, result.TopError.Code);
            return result.Errors;
        }

        _logger.LogInformation("Profile updated for user {UserId}", userId);
        return result.Value;
    }
}
