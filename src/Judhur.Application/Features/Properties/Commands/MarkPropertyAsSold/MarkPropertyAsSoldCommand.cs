using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Properties.Commands.MarkPropertyAsSold;

public sealed record MarkPropertyAsSoldCommand(Guid PropertyId) : IInvalidateCacheCommand<Result<Updated>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}
