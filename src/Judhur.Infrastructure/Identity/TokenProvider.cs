using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Judhur.Application.Common.Interfaces;
using Judhur.Application.Features.Identity;
using Judhur.Application.Features.Identity.Dtos;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Judhur.Infrastructure.Identity;

public sealed class TokenProvider(
    JwtSettings jwtSettings,
    IAppDbContext context,
    TimeProvider timeProvider) : ITokenProvider
{
    private const int RefreshTokenSizeInBytes = 32;

    private readonly JwtSettings _jwtSettings = jwtSettings;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var expiresOnUtc = nowUtc.AddMinutes(_jwtSettings.TokenExpirationInMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresOnUtc.UtcDateTime,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                SecurityAlgorithms.HmacSha256Signature),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(descriptor));

        var refreshTokenResult = RefreshToken.Create(
            Guid.CreateVersion7(),
            GenerateRefreshToken(),
            user.UserId,
            nowUtc.AddDays(_jwtSettings.RefreshTokenExpirationInDays),
            nowUtc);

        if (refreshTokenResult.IsError)
        {
            return refreshTokenResult.Errors;
        }

        // One active refresh token per user. ExecuteDeleteAsync runs against the database
        // immediately rather than waiting for SaveChanges, so the old tokens are gone the
        // moment this line runs -- it is only safe to call inside a transaction.
        await _context.RefreshTokens
            .Where(refreshToken => refreshToken.UserId == user.UserId)
            .ExecuteDeleteAsync(ct);

        _context.RefreshTokens.Add(refreshTokenResult.Value);
        await _context.SaveChangesAsync(ct);

        return new TokenResponse(accessToken, refreshTokenResult.Value.Token, expiresOnUtc);
    }

    private static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenSizeInBytes));
}
