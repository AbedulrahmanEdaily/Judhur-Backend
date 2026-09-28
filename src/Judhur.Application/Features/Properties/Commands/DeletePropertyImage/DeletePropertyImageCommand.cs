using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Properties.Commands.DeletePropertyImage;

public sealed record DeletePropertyImageCommand(Guid PropertyId, Guid ImageId) : IInvalidateCacheCommand<Result<Deleted>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}