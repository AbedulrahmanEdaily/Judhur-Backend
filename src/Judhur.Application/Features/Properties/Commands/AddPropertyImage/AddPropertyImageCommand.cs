using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Properties.Commands.AddPropertyImage;

public sealed record AddPropertyImageCommand(
    Guid PropertyId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    bool IsMainImage) : IInvalidateCacheCommand<Result<PropertyImageDto>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}