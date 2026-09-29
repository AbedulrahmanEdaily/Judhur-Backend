using System.Text.Json.Serialization;

using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Properties.Commands.UpdatePropertyDescription;

public sealed record UpdatePropertyDescriptionCommand(
    [property: JsonIgnore] Guid PropertyId,
    string? Description
) : IInvalidateCacheCommand<Result<Updated>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}
