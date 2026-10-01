using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.RemoveProfileImage;

public sealed class RemoveProfileImageCommandHandler(IUser user, IIdentityService identityService, IFileStorage fileStorage, ILogger<RemoveProfileImageCommandHandler> logger)
    : IRequestHandler<RemoveProfileImageCommand, Result<Deleted>>
{
    private readonly IUser _user = user;
    private readonly IIdentityService _identityService = identityService;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly ILogger<RemoveProfileImageCommandHandler> _logger = logger;

    public async Task<Result<Deleted>> Handle(RemoveProfileImageCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Remove profile image rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var removeResult = await _identityService.SetProfileImageAsync(userId, null, cancellationToken);
        if (removeResult.IsError)
        {
            _logger.LogWarning("Remove profile image failed for user {UserId}: {ErrorCode}", userId, removeResult.TopError.Code);
            return removeResult.Errors;
        }

        var previous = removeResult.Value;
        if (previous.PreviousUrl is null)
        {
            _logger.LogDebug("User {UserId} has no profile image to remove", userId);
            return Result.Deleted;
        }

        if (previous.PreviousPublicId is { } previousPublicId)
        {
            var deleteResult = await _fileStorage.DeleteFileAsync(previousPublicId, CancellationToken.None);
            if (deleteResult.IsError)
            {
                _logger.LogWarning("Failed to delete profile image {PublicId} for user {UserId}: {ErrorCode}", previousPublicId, userId, deleteResult.TopError.Code);
            }
        }

        _logger.LogInformation("Profile image removed for user {UserId}", userId);
        return Result.Deleted;
    }
}
