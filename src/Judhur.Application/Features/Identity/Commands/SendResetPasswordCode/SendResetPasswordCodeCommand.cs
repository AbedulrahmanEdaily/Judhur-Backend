using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.SendResetPasswordCode;

public sealed record SendResetPasswordCodeCommand(string Email) : IRequest<Result<Success>>;
