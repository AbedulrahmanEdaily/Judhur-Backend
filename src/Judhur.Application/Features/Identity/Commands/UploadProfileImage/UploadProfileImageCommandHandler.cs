using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.UploadProfileImage;

public sealed class UploadProfileImageCommandHandler(IUser user, IIdentityService identityService, IFileStorage fileStorage, ILogger<UploadProfileImageCommandHandler> logger)
    : IRequestHandler<UploadProfileImageCommand, Result<ProfileImageDto>>
{
    private readonly IUser _user = user;
    private readonly IIdentityService _identityService = identityService;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly ILogger<UploadProfileImageCommandHandler> _logger = logger;

    public async Task<Result<ProfileImageDto>> Handle(UploadProfileImageCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Upload profile image rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var uploadResult = await _fileStorage.UploadImageAsync(
            request.Content,
            request.FileName,
            $"judhur/users/{userId}",
            cancellationToken);
        if (uploadResult.IsError)
        {
            _logger.LogWarning("Upload profile image failed for user {UserId}: {ErrorCode}", userId, uploadResult.TopError.Code);
            return uploadResult.Errors;
        }
        var storedFile = uploadResult.Value;

        Result<ReplacedProfileImage> replaceResult;
        try
        {
            replaceResult = await _identityService.SetProfileImageAsync(userId, storedFile, cancellationToken);
        }
        catch
        {
            await _fileStorage.DeleteFileAsync(storedFile.PublicId, CancellationToken.None);
            throw;
        }
        if (replaceResult.IsError)
        {
            _logger.LogWarning("Saving profile image failed for user {UserId}: {ErrorCode}", userId, replaceResult.TopError.Code);
            await _fileStorage.DeleteFileAsync(storedFile.PublicId, CancellationToken.None);
            return replaceResult.Errors;
        }

        if (replaceResult.Value.PreviousPublicId is { } previousPublicId)
        {
            var deleteResult = await _fileStorage.DeleteFileAsync(previousPublicId, CancellationToken.None);
            if (deleteResult.IsError)
            {
                _logger.LogWarning("Failed to delete previous profile image {PublicId} for user {UserId}: {ErrorCode}", previousPublicId, userId, deleteResult.TopError.Code);
            }
        }

        _logger.LogInformation("Profile image updated for user {UserId}", userId);
        return new ProfileImageDto(storedFile.Url);
    }
}
