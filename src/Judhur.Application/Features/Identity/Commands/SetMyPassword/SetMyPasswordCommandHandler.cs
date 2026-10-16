using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Application.Features.Identity.Events;
using Judhur.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.SetMyPassword;

public sealed class SetMyPasswordCommandHandler(
    IUser user,
    IAppDbContext context,
    IIdentityService identityService,
    ITokenProvider tokenProvider,
    IDeferredDispatcher dispatcher,
    IPublisher publisher,
    ILogger<SetMyPasswordCommandHandler> logger) : IRequestHandler<SetMyPasswordCommand, Result<TokenResponse>>
{
    private readonly IUser _user = user;
    private readonly IAppDbContext _context = context;
    private readonly IIdentityService _identityService = identityService;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IDeferredDispatcher _dispatcher = dispatcher;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<SetMyPasswordCommandHandler> _logger = logger;

    public async Task<Result<TokenResponse>> Handle(SetMyPasswordCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is not { } userId)
        {
            _logger.LogWarning("Set my password rejected: request has no authenticated user");
            return ApplicationError.Unauthenticated;
        }

        var result = await _identityService.SetPasswordAsync(userId, request.CurrentPassword, request.NewPassword, request.GoogleIdToken, cancellationToken);
        if (result.IsError)
        {
            _logger.LogWarning("Set my password failed for user {UserId}: {ErrorCode}", userId, result.TopError.Code);
            return result.Errors;
        }

        var revokedSessions = await _context.RefreshTokens
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        var userResult = await _identityService.GetUserByIdAsync(userId.ToString(), cancellationToken);
        if (userResult.IsError)
        {
            _logger.LogWarning("Set my password for user {UserId} could not issue new tokens: {ErrorCode}", userId, userResult.TopError.Code);
            return userResult.Errors;
        }
        var tokens = await _tokenProvider.GenerateJwtTokenAsync(userResult.Value, cancellationToken);
        if (tokens.IsError)
        {
            _logger.LogError("Token generation failed for user {UserId} after a password change: {ErrorCode}", userId, tokens.TopError.Code);
            return tokens.Errors;
        }

        _dispatcher.Defer(ct => _publisher.Publish(new PasswordChanged(userId), ct));
        _logger.LogInformation("Password set for user {UserId}: {RevokedSessions} other sessions ended", userId, revokedSessions);
        return tokens.Value;
    }
}
