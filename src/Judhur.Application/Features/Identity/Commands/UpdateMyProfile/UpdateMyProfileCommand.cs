using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Identity.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    string FullName,
    string PhoneNumber,
    string City,
    string? Bio) : IInvalidateCacheCommand<Result<MyProfileDto>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}
