using Microsoft.Extensions.Configuration;

namespace Judhur.Infrastructure.Data;

public sealed class AdminSeedSettings
{
    public const string SectionName = "AdminSeed";

    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FullName { get; init; }
    public required string City { get; init; }

    public static AdminSeedSettings Bind(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(SectionName);

        return new AdminSeedSettings
        {
            Email = Required(section, nameof(Email)),
            Password = Required(section, nameof(Password)),
            FullName = Required(section, nameof(FullName)),
            City = Required(section, nameof(City))
        };
    }

    private static string Required(IConfigurationSection section, string key)
        => section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Configuration value '{section.Path}:{key}' is missing.");
}
