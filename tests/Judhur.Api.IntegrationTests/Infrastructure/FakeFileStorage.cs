using System.Collections.Concurrent;

using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

namespace Judhur.Api.IntegrationTests.Infrastructure;

public sealed class FakeFileStorage : IFileStorage
{
    public const string InvalidContentMarker = "not-an-image";

    private readonly ConcurrentDictionary<string, byte> _files = new();
    private readonly ConcurrentQueue<string> _deleted = new();

    public IReadOnlyCollection<string> Files => _files.Keys.ToArray();

    public IReadOnlyCollection<string> Deleted => _deleted.ToArray();

    public async Task<Result<StoredFile>> UploadImageAsync(Stream content, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        if (await IsInvalidAsync(content, cancellationToken))
        {
            return ApplicationError.InvalidFileContent;
        }
        var publicId = $"{folder}/{Guid.NewGuid():N}";
        _files[publicId] = 0;
        return new StoredFile($"https://files.test/{publicId}.jpg", publicId);
    }

    public Task<Result<Deleted>> DeleteFileAsync(string publicId, CancellationToken cancellationToken = default)
    {
        _files.TryRemove(publicId, out _);
        _deleted.Enqueue(publicId);
        return Task.FromResult<Result<Deleted>>(Result.Deleted);
    }

    public async Task<Result<string>> UploadPrivateDocumentAsync(Stream content, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        if (await IsInvalidAsync(content, cancellationToken))
        {
            return ApplicationError.InvalidFileContent;
        }
        var publicId = $"{folder}/{Guid.NewGuid():N}";
        _files[publicId] = 0;
        return publicId;
    }

    public Task<Result<Deleted>> DeletePrivateDocumentAsync(string publicId, CancellationToken cancellationToken = default)
        => DeleteFileAsync(publicId, cancellationToken);

    public string GetPrivateDocumentUrl(string publicId, DateTimeOffset expiresAtUtc)
        => $"https://files.test/private/{publicId}?expires={expiresAtUtc.ToUnixTimeSeconds()}";

    private static async Task<bool> IsInvalidAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        return text.Contains(InvalidContentMarker, StringComparison.Ordinal);
    }
}
