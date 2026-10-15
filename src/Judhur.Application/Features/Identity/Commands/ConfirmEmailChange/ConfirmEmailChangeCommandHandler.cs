using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.ConfirmEmailChange;

public sealed class ConfirmEmailChangeCommandHandler(
    IAppDbContext context,
    IIdentityService identityService,
    IDeferredDispatcher dispatcher,
    IPublisher publisher,
    ILogger<ConfirmEmailChangeCommandHandler> logger) : IRequestHandler<ConfirmEmailChangeCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;
    private readonly IIdentityService _identityService = identityService;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<ConfirmEmailChangeCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ConfirmEmailChangeAsync(request.UserId, request.NewEmail.Trim(), request.Token, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Confirm email change failed for user {UserId}: {ErrorCode}", request.UserId, result.TopError.Code);
            return result.Errors;
        }

        var revokedSessions = await _context.RefreshTokens
            .Where(r => r.UserId == request.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        var changed = new EmailChanged(request.UserId, result.Value.OldEmail);
        _dispatcher.Defer(ct => _publisher.Publish(changed, ct));
        _logger.LogInformation("Email changed for user {UserId}: {RevokedSessions} sessions revoked", request.UserId, revokedSessions);
        return Result.Success;
    }
}
