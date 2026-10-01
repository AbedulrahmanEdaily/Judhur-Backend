using Microsoft.Extensions.Configuration;

namespace Judhur.Infrastructure.Identity.GoogleLogin;

public sealed class GoogleSettings
{
    public const string SectionName = "Google";

    public required string ClientId { get; init; }

    public static GoogleSettings Bind(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(SectionName);

        return new GoogleSettings
        {
            ClientId = section[nameof(ClientId)] is { Length: > 0 } value
                ? value
                : throw new InvalidOperationException($"Configuration value '{section.Path}:{nameof(ClientId)}' is missing.")
        };
    }
}
