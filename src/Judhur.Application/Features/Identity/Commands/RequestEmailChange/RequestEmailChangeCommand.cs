using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.RequestEmailChange;

public sealed record RequestEmailChangeCommand(string NewEmail, string? Password) : IRequest<Result<Success>>;
