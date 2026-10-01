
using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler(IUser user, IIdentityService identityService, ILogger<GetMyProfileQueryHandler> logger) : IRequestHandler<GetMyProfileQuery, Result<MyProfileDto>>
{
    private readonly IUser _user = user;
    private readonly IIdentityService _identityService = identityService;
    private readonly ILogger<GetMyProfileQueryHandler> _logger = logger;

    public async Task<Result<MyProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Get my profile rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }
        var result = await _identityService.GetMyProfileAsync(userId, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Get my profile failed for user {UserId}: {ErrorCode}", userId, result.TopError.Code);
            return result.Errors;
        }
        _logger.LogDebug("Profile retrieved for user {UserId}", userId);
        return result.Value;
    }
}