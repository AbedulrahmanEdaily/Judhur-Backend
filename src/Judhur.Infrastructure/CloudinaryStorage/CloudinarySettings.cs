using Microsoft.Extensions.Configuration;

namespace Judhur.Infrastructure.CloudinaryStorage;

public sealed class CloudinarySettings
{
    public const string SectionName = "CloudinarySettings";

    public required string CloudName { get; init; }
    public required string ApiKey { get; init; }
    public required string ApiSecret { get; init; }

    public static CloudinarySettings Bind(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        return new CloudinarySettings
        {
            CloudName = RequiredValue(section, nameof(CloudName)),
            ApiKey = RequiredValue(section, nameof(ApiKey)),
            ApiSecret = RequiredValue(section, nameof(ApiSecret))
        };
    }

    private static string RequiredValue(IConfigurationSection section, string key)
        => section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Configuration value '{section.Path}:{key}' is missing.");
}
