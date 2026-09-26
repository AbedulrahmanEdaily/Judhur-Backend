using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(IIdentityService identityService, ILogger<ConfirmEmailCommandHandler> logger) : IRequestHandler<ConfirmEmailCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ConfirmEmailAsync(request.UserId, request.Token, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Email confirmation failed for user {UserId}: {ErrorCode}", request.UserId, result.TopError.Code);
            return result.Errors;
        }
        _logger.LogInformation("Email confirmed for user {UserId}", request.UserId);
        return Result.Success;
    }
}