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
    private readonly ICloudinary _cloudinary = cloudinary;
    private readonly ILogger<CloudinaryFileStorage> _logger = logger;

    public async Task<Result<Deleted>> DeleteFileAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
        if (result.Result is "ok" or "not found")
        {
            _logger.LogInformation("File {PublicId} deleted ({Result})", publicId, result.Result);
            return Result.Deleted;
        }
        _logger.LogError("Deleting file {PublicId} failed: {Result} {Error}", publicId, result.Result, result.Error?.Message);
        return ApplicationError.DeleteFailed;
    }

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
}
