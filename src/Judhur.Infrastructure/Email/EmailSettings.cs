using Microsoft.Extensions.Configuration;

namespace Judhur.Infrastructure.Email;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public required string SenderAddress { get; init; }
    public required string SenderName { get; init; }
    public required string SmtpHost { get; init; }
    public required int SmtpPort { get; init; }

    public static EmailSettings Bind(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(SectionName);

        return new EmailSettings
        {
            SenderAddress = Required(section, nameof(SenderAddress)),
            SenderName = Required(section, nameof(SenderName)),
            SmtpHost = Required(section, nameof(SmtpHost)),
            SmtpPort = int.TryParse(section[nameof(SmtpPort)], out var port)
                ? port
                : throw new InvalidOperationException($"Configuration value '{section.Path}:{nameof(SmtpPort)}' is missing or invalid.")
        };
    }

    private static string Required(IConfigurationSection section, string key)
        => section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Configuration value '{section.Path}:{key}' is missing.");
}
