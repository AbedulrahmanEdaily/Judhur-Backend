using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken, string ExpiredAccessToken) : IRequest<Result<TokenResponse>>;