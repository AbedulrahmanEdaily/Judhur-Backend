using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ConfirmEmailChange;

public sealed record ConfirmEmailChangeCommand(Guid UserId, string NewEmail, string Token) : IRequest<Result<Success>>, ITransactionalCommand;
