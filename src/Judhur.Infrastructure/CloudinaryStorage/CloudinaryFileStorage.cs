using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

using Microsoft.Extensions.Logging;

namespace Judhur.Infrastructure.CloudinaryStorage;

public sealed class CloudinaryFileStorage(ICloudinary cloudinary, ILogger<CloudinaryFileStorage> logger) : IFileStorage
{
    private const string PrivateType = "private";
    private const string RawResourceType = "raw";

    private readonly ICloudinary _cloudinary = cloudinary;
    private readonly ILogger<CloudinaryFileStorage> _logger = logger;

    public async Task<Result<StoredFile>> UploadImageAsync(Stream content, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = folder
        };
        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
        if (result.Error is not null)
        {
            _logger.LogError("Image upload to folder {Folder} failed: {Error}", folder, result.Error.Message);
            return ApplicationError.UploadFailed;
        }
        _logger.LogInformation("Image uploaded as {PublicId}", result.PublicId);
        return new StoredFile(result.SecureUrl.ToString(), result.PublicId);
    }

    public Task<Result<Deleted>> DeleteFileAsync(string publicId, CancellationToken cancellationToken = default)
    {
        return DestroyAsync(new DeletionParams(publicId));
    }

    public async Task<Result<string>> UploadPrivateDocumentAsync(Stream content, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = folder,
            Type = PrivateType
        };
        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken: cancellationToken);
        if (result.Error is not null)
        {
            _logger.LogError("Private document upload to folder {Folder} failed: {Error}", folder, result.Error.Message);
            return ApplicationError.UploadFailed;
        }
        _logger.LogInformation("Private document uploaded as {PublicId}", result.PublicId);
        return result.PublicId;
    }

    public Task<Result<Deleted>> DeletePrivateDocumentAsync(string publicId, CancellationToken cancellationToken = default)
    {
        return DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Raw,
            Type = PrivateType
        });
    }

    public string GetPrivateDocumentUrl(string publicId, DateTimeOffset expiresAtUtc)
    {
        return _cloudinary.DownloadPrivate(
            publicId,
            attachment: false,
            type: PrivateType,
            expiresAt: expiresAtUtc.ToUnixTimeSeconds(),
            resourceType: RawResourceType);
    }

    private async Task<Result<Deleted>> DestroyAsync(DeletionParams deletionParams)
    {
        var result = await _cloudinary.DestroyAsync(deletionParams);
        if (result.Result is "ok" or "not found")
        {
            _logger.LogInformation("File {PublicId} deleted ({Result})", deletionParams.PublicId, result.Result);
            return Result.Deleted;
        }
        _logger.LogError("Deleting file {PublicId} failed: {Result} {Error}", deletionParams.PublicId, result.Result, result.Error?.Message);
        return ApplicationError.DeleteFailed;
    }
}
