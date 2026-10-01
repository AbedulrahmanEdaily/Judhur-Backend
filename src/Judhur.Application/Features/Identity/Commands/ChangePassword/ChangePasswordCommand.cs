using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string Code, string Password, string Email) : IRequest<Result<Success>>;
