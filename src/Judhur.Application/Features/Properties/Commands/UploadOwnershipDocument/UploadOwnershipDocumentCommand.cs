using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Properties.Commands.UploadOwnershipDocument;

public sealed record UploadOwnershipDocumentCommand(
    Guid PropertyId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length) : IInvalidateCacheCommand<Result<Updated>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}
