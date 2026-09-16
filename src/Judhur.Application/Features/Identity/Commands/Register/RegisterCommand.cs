using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed record RegisterCommand(
    string UserName,
    string FullName,
    string Email,
    string PhoneNumber,
    string City,
    string Password) : IRequest<Result<TokenResponse>>, ITransactionalCommand;
