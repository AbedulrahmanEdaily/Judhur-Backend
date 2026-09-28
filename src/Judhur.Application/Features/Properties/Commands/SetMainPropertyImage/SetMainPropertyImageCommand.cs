using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Properties.Commands.SetMainPropertyImage;

public sealed record SetMainPropertyImageCommand(Guid PropertyId, Guid ImageId) : IInvalidateCacheCommand<Result<Updated>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}