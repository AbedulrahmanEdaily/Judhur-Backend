using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Properties.Commands.ApproveProperty;

public sealed record ApprovePropertyCommand(Guid PropertyId) : IInvalidateCacheCommand<Result<Updated>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}