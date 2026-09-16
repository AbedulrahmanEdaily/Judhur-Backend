using Microsoft.Extensions.Configuration;

namespace Judhur.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    private const int MinimumSecretLength = 32;

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string Secret { get; init; }

    public required int TokenExpirationInMinutes { get; init; }

    public int RefreshTokenExpirationInDays { get; init; } = 7;

    public static JwtSettings Bind(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        var secret = RequiredValue(section, nameof(Secret));
        if (secret.Length < MinimumSecretLength)
        {
            throw new InvalidOperationException(
                $"Configuration value '{section.Path}:{nameof(Secret)}' must be at least {MinimumSecretLength} characters long.");
        }

        return new JwtSettings
        {
            Issuer = RequiredValue(section, nameof(Issuer)),
            Audience = RequiredValue(section, nameof(Audience)),
            Secret = secret,
            TokenExpirationInMinutes = RequiredPositiveInt(section, nameof(TokenExpirationInMinutes)),
            RefreshTokenExpirationInDays = section[nameof(RefreshTokenExpirationInDays)] is { Length: > 0 } days
                ? RequiredPositiveInt(section, nameof(RefreshTokenExpirationInDays))
                : 7,
        };
    }

    private static string RequiredValue(IConfigurationSection section, string key)
        => section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Configuration value '{section.Path}:{key}' is missing.");

    private static int RequiredPositiveInt(IConfigurationSection section, string key)
    {
        var raw = RequiredValue(section, key);

        if (!int.TryParse(raw, out var value) || value <= 0)
        {
            throw new InvalidOperationException(
                $"Configuration value '{section.Path}:{key}' must be an integer greater than zero, but was '{raw}'.");
        }

        return value;
    }
}
