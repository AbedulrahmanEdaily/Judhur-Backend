using System.Text.Json.Serialization;

using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Properties.Commands.ApproveProperty;

public sealed record ApprovePropertyCommand([property: JsonIgnore] Guid PropertyId, string RowVersion) : IInvalidateCacheCommand<Result<Updated>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}
