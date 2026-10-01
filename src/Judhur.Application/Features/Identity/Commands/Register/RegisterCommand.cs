using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed record RegisterCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string City,
    string? Bio,
    string Password) : IRequest<Result<Success>>, ITransactionalCommand;
