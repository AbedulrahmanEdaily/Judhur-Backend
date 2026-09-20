using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<TokenResponse>>;
