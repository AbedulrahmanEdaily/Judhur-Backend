using System.Security.Claims;

using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Identity;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Judhur.Application.Features.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(IAppDbContext context, ITokenProvider tokenProvider, ILogger<RefreshTokenCommandHandler> logger, IIdentityService identityService, TimeProvider timeProvider) : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    private readonly IAppDbContext _context = context;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly ILogger<RefreshTokenCommandHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _tokenProvider.GetPrincipalFromExpiredToken(request.ExpiredAccessToken);
        if (principal is null)
        {
            _logger.LogWarning("Refresh rejected: expired access token is not valid");
            return ApplicationError.ExpiredAccessTokenInvalid;
        }
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Refresh rejected: access token has no valid user id claim");
            return ApplicationError.UserIdClaimInvalid;
        }
        var getUserResult = await _identityService.GetUserByIdAsync(userId.ToString(), cancellationToken);
        if (getUserResult.IsError)
        {
            _logger.LogWarning("Refresh rejected for user {UserId}: {ErrorCode}", userId, getUserResult.TopError.Code);
            return getUserResult.Errors;
        }
        var nowUtc = _timeProvider.GetUtcNow();
        var claimedTokens = await _context.RefreshTokens
            .Where(r => r.Token == request.RefreshToken && r.UserId == userId && r.ExpiresOnUtc > nowUtc)
            .ExecuteDeleteAsync(cancellationToken);
        if (claimedTokens == 0)
        {
            _logger.LogWarning("Refresh rejected for user {UserId}: refresh token missing or expired", userId);
            return RefreshTokenErrors.Expired;
        }
        var generateTokenResult = await _tokenProvider.GenerateJwtTokenAsync(getUserResult.Value, cancellationToken);
        if (generateTokenResult.IsError)
        {
            _logger.LogError("Token generation failed for user {UserId}: {ErrorCode}", userId, generateTokenResult.TopError.Code);
            return generateTokenResult.Errors;
        }
        _logger.LogInformation("Tokens refreshed for user {UserId}", userId);
        return generateTokenResult.Value;
    }
}
