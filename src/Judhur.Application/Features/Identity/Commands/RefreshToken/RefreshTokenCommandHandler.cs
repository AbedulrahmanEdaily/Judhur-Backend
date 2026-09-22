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
            _logger.LogError("Expired access token is not valid");
            return ApplicationError.ExpiredAccessTokenInvalid;
        }
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogError("Invalid userId claim");
            return ApplicationError.UserIdClaimInvalid;
        }
        var getUserResult = await _identityService.GetUserByIdAsync(userId.ToString(), cancellationToken);
        if (getUserResult.IsError)
        {
            _logger.LogError("Get user by id error occurred: {ErrorDescription}", getUserResult.TopError.Description);
            return getUserResult.Errors;
        }
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == request.RefreshToken && r.UserId == userId, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow();
        if (refreshToken is null || refreshToken.IsExpired(nowUtc))
        {
            _logger.LogError("Refresh token has expired");
            return RefreshTokenErrors.Expired;
        }
        var generateTokenResult = await _tokenProvider.GenerateJwtTokenAsync(getUserResult.Value, cancellationToken);
        if (generateTokenResult.IsError)
        {
            _logger.LogError("Generate token error occurred: {ErrorDescription}", generateTokenResult.TopError.Description);
            return generateTokenResult.Errors;
        }
        return generateTokenResult.Value;
    }
}
