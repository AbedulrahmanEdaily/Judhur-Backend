using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Identity.Commands.DeleteMyAccount;

public sealed record DeleteMyAccountCommand(string? Password) : IInvalidateCacheCommand<Result<Deleted>>, ITransactionalCommand
{
    string[] IInvalidateCacheCommand.Tags => ["properties", "sellers"];
}
