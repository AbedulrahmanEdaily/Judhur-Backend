using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Identity.Commands.UploadProfileImage;

public sealed record UploadProfileImageCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length) : IInvalidateCacheCommand<Result<ProfileImageDto>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}
