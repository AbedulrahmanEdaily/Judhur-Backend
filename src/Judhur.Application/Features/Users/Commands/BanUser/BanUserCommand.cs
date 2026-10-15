using System.Text.Json.Serialization;

using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Users.Commands.BanUser;

public sealed record BanUserCommand([property: JsonIgnore] Guid UserId, string Reason)
    : IInvalidateCacheCommand<Result<Updated>>, ITransactionalCommand
{
    string[] IInvalidateCacheCommand.Tags => ["properties", "sellers"];
}
