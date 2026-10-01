using Google.Apis.Auth;

using Judhur.Application.Common;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

using Microsoft.Extensions.Logging;

namespace Judhur.Infrastructure.Identity.GoogleLogin;

public sealed class GoogleTokenValidator(GoogleSettings settings, ILogger<GoogleTokenValidator> logger) : IGoogleTokenValidator
{
    private readonly GoogleSettings _settings = settings;
    private readonly ILogger<GoogleTokenValidator> _logger = logger;

    public async Task<Result<GoogleUser>> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_settings.ClientId]
            });
        }
        catch (InvalidJwtException exception)
        {
            _logger.LogWarning("Google token rejected: {Reason}", exception.Message);
            return ApplicationError.InvalidGoogleToken;
        }

        if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
        {
            _logger.LogWarning("Google token rejected: the subject or the email is missing");
            return ApplicationError.InvalidGoogleToken;
        }

        var fullName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name;
        return new GoogleUser(payload.Subject, payload.Email, payload.EmailVerified, fullName, payload.Picture);
    }
}
