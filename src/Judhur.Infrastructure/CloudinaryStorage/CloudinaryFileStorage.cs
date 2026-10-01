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
        var signature = await ReadSignatureAsync(content, cancellationToken);
        if (!IsImage(signature))
        {
            _logger.LogWarning("Image upload to folder {Folder} rejected: content is not a JPG, PNG or WEBP image", folder);
            return ApplicationError.InvalidFileContent;
        }
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = folder
        };
        ImageUploadResult result;
        try
        {
            result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Image upload to folder {Folder} threw", folder);
            return ApplicationError.UploadFailed;
        }
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
        var signature = await ReadSignatureAsync(content, cancellationToken);
        if (!IsImage(signature) && !IsPdf(signature))
        {
            _logger.LogWarning("Private document upload to folder {Folder} rejected: content is not a PDF or an image", folder);
            return ApplicationError.InvalidFileContent;
        }
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = folder,
            Type = PrivateType
        };
        RawUploadResult result;
        try
        {
            result = await _cloudinary.UploadAsync(uploadParams, cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Private document upload to folder {Folder} threw", folder);
            return ApplicationError.UploadFailed;
        }
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
        DeletionResult result;
        try
        {
            result = await _cloudinary.DestroyAsync(deletionParams);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Deleting file {PublicId} threw", deletionParams.PublicId);
            return ApplicationError.DeleteFailed;
        }
        if (result.Result is "ok" or "not found")
        {
            _logger.LogInformation("File {PublicId} deleted ({Result})", deletionParams.PublicId, result.Result);
            return Result.Deleted;
        }
        _logger.LogError("Deleting file {PublicId} failed: {Result} {Error}", deletionParams.PublicId, result.Result, result.Error?.Message);
        return ApplicationError.DeleteFailed;
    }

    private static async Task<byte[]> ReadSignatureAsync(Stream content, CancellationToken cancellationToken)
    {
        var buffer = new byte[12];
        var read = 0;
        while (read < buffer.Length)
        {
            var count = await content.ReadAsync(buffer.AsMemory(read), cancellationToken);
            if (count == 0)
            {
                break;
            }
            read += count;
        }
        content.Position = 0;
        return buffer[..read];
    }

    private static bool IsImage(byte[] signature)
        => StartsWith(signature, [0xFF, 0xD8, 0xFF])
            || StartsWith(signature, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])
            || (StartsWith(signature, "RIFF"u8) && signature.Length >= 12 && signature.AsSpan(8, 4).SequenceEqual("WEBP"u8));

    private static bool IsPdf(byte[] signature)
        => StartsWith(signature, "%PDF"u8);

    private static bool StartsWith(byte[] signature, ReadOnlySpan<byte> prefix)
        => signature.AsSpan().StartsWith(prefix);
}
