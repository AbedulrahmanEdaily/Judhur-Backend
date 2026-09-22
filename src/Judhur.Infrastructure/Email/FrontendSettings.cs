using Microsoft.Extensions.Configuration;
namespace Judhur.Infrastructure.Email;

public sealed class FrontendSettings
{
    public const string SectionName = "Frontend";
    public required string ConfirmEmailUrl { get; init; }
    public static FrontendSettings Bind(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(SectionName);

        return new FrontendSettings
        {
            ConfirmEmailUrl = section[nameof(ConfirmEmailUrl)] is { Length: > 0 } value ? 
            value : 
            throw new InvalidOperationException($"Configuration value '{section.Path}:{nameof(ConfirmEmailUrl)}' is missing.")
        };
    }
}