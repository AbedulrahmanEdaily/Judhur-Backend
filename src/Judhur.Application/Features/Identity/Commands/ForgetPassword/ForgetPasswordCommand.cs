using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ForgetPassword;

public sealed record ForgetPasswordCommand(string Email) : IRequest<Result<Success>>;