using System.Net;
using System.Reflection;

namespace Judhur.Infrastructure.Email;

internal static class EmailTemplates
{
    public const string LogoContentId = "judhur-logo";

    private static readonly Assembly Assembly = typeof(EmailTemplates).Assembly;

    public static string Render(string templateName, Dictionary<string, string> values)
    {
        using var stream = Open($"{templateName}.html");
        using var reader = new StreamReader(stream);
        var html = reader.ReadToEnd();

        foreach (var (key, value) in values)
        {
            html = html.Replace("{{" + key + "}}", WebUtility.HtmlEncode(value));
        }

        return html;
    }

    public static Stream OpenLogo() => Open("judhur-logo.png");

    private static Stream Open(string fileName)
        => Assembly.GetManifestResourceStream($"EmailTemplates.{fileName}")
            ?? throw new InvalidOperationException($"Email template resource '{fileName}' was not found.");
}
