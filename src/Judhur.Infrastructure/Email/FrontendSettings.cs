using Microsoft.Extensions.Configuration;
namespace Judhur.Infrastructure.Email;

public sealed class FrontendSettings
{
    public const string SectionName = "Frontend";
    public required string ConfirmEmailUrl { get; init; }
    public required string ConfirmEmailChangeUrl { get; init; }
    public static FrontendSettings Bind(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(SectionName);

        var confirmEmailUrl = section[nameof(ConfirmEmailUrl)] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Configuration value '{section.Path}:{nameof(ConfirmEmailUrl)}' is missing.");

        return new FrontendSettings
        {
            ConfirmEmailUrl = confirmEmailUrl,
            ConfirmEmailChangeUrl = section[nameof(ConfirmEmailChangeUrl)] is { Length: > 0 } changeUrl
                ? changeUrl
                : new Uri(new Uri(confirmEmailUrl), "/confirm-email-change").ToString()
        };
    }
}