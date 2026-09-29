using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<Result<StoredFile>> UploadImageAsync(Stream content, string fileName, string folder, CancellationToken cancellationToken = default);

    Task<Result<Deleted>> DeleteFileAsync(string publicId, CancellationToken cancellationToken = default);

    Task<Result<string>> UploadPrivateDocumentAsync(Stream content, string fileName, string folder, CancellationToken cancellationToken = default);

    Task<Result<Deleted>> DeletePrivateDocumentAsync(string publicId, CancellationToken cancellationToken = default);

    string GetPrivateDocumentUrl(string publicId, DateTimeOffset expiresAtUtc);
}
