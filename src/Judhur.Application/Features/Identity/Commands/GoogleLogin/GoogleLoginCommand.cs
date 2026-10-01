using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(string IdToken, string? PhoneNumber, string? City)
    : IRequest<Result<TokenResponse>>, ITransactionalCommand;
