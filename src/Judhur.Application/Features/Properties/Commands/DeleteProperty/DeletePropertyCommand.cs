using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Properties.Commands.DeleteProperty;

public sealed record DeletePropertyCommand(Guid PropertyId) : IInvalidateCacheCommand<Result<Deleted>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}
