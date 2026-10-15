using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.SetMyPassword;

public sealed record SetMyPasswordCommand(string? CurrentPassword, string NewPassword, string? GoogleIdToken = null) : IRequest<Result<Success>>;
