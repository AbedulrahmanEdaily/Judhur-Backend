using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Token) : IRequest<Result<Success>>;