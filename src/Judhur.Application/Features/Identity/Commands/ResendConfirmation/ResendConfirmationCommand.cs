using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.ResendConfirmation;

public sealed record ResendConfirmationCommand(string Email) : IRequest<Result<Success>>;
