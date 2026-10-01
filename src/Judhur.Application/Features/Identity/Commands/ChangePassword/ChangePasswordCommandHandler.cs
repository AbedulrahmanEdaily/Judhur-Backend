using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IIdentityService identityService,
    IAppDbContext context,
    IDeferredDispatcher dispatcher,
    IPublisher publisher,
    ILogger<ChangePasswordCommandHandler> logger) : IRequestHandler<ChangePasswordCommand, Result<Success>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IAppDbContext _context = context;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<ChangePasswordCommandHandler> _logger = logger;

    public async Task<Result<Success>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ChangePasswordAsync(request.Email, request.Password, request.Code, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Change password failed: {ErrorCode}", result.TopError.Code);
            return result.Errors;
        }
        var userId = result.Value;
        var revokedSessions = await _context.RefreshTokens
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        _dispatcher.Defer(ct => _publisher.Publish(new PasswordChanged(userId), ct));
        _logger.LogInformation("Password changed for user {UserId}, {RevokedSessions} sessions revoked", userId, revokedSessions);
        return Result.Success;
    }
}
