namespace Judhur.Application.Features.Identity;

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresOnUtc);
