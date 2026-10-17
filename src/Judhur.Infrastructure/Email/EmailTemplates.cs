using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Judhur.Infrastructure.Email;

internal static partial class EmailTemplates
{
    public const string LogoContentId = "judhur-logo";

    private static readonly Assembly Assembly = typeof(EmailTemplates).Assembly;

    public static string Render(string templateName, Dictionary<string, string> values)
    {
        using var stream = Open($"{templateName}.html");
        using var reader = new StreamReader(stream);
        var html = reader.ReadToEnd();

        return Placeholder().Replace(html, match => values.TryGetValue(match.Groups[1].Value, out var value)
            ? WebUtility.HtmlEncode(value)
            : match.Value);
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex Placeholder();

    public static Stream OpenLogo() => Open("judhur-logo.png");

    private static Stream Open(string fileName)
        => Assembly.GetManifestResourceStream($"EmailTemplates.{fileName}")
            ?? throw new InvalidOperationException($"Email template resource '{fileName}' was not found.");
}
