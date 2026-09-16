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
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Judhur.Infrastructure.Identity;

public class TokenProvider
{
    private readonly IConfiguration _configuration;
    private readonly IAppDbContext _context;

    public TokenProvider(IConfiguration configuration, IAppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }
    public async Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct)
    {
        var tokenResult = await CreateAsync(user, ct);
        if (tokenResult.IsError)
        {
            return tokenResult.Errors;
        }
        return tokenResult.Value;
    }

    private async Task<Result<TokenResponse>> CreateAsync(AppUserDto user, CancellationToken ct)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var key = jwtSettings["Secret"]!;
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["TokenExpirationInMinutes"]!));
        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub ,user.UserId!.ToString()),
            new (JwtRegisteredClaimNames.Email ,user.Email!),
        };
        foreach (var role in user.Roles)
        {
            claims.Add(new(ClaimTypes.Role, role));
        }
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        SecurityAlgorithms.HmacSha256Signature),
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(descriptor);
        var oldRefreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.UserId)
                .ExecuteDeleteAsync(ct);
        var dateTime = DateTime.UtcNow.AddDays(7);
        var refreshTokenResult = RefreshToken.Create(Guid.CreateVersion7(), GenerateRefreshToken(), user.UserId, dateTime, dateTime);
        if (refreshTokenResult.IsError)
        {
            return refreshTokenResult.Errors;
        }
        var refreshToken = refreshTokenResult.Value;
        await _context.SaveChangesAsync(ct);
        return new TokenResponse
        {
            AccessToken = tokenHandler.WriteToken(securityToken),
            RefreshToken = refreshToken.Token,
            ExpireOnUtc = expires
        };
    }
    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}