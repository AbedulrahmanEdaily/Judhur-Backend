using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Identity.Commands.RemoveProfileImage;

public sealed record RemoveProfileImageCommand : IInvalidateCacheCommand<Result<Deleted>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}
