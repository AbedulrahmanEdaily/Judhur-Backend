using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.SetMyPassword;

public sealed record SetMyPasswordCommand(string? CurrentPassword, string NewPassword, string? GoogleIdToken = null) : IRequest<Result<TokenResponse>>, ITransactionalCommand;
