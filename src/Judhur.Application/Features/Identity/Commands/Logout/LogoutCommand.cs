using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result<Success>>;
