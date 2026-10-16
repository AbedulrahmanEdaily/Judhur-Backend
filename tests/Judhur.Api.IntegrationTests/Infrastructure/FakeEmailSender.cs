using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;

namespace Judhur.Api.IntegrationTests.Infrastructure;

public sealed partial class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyCollection<EmailMessage> Sent => _sent.ToArray();

    public Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public async Task<EmailMessage> WaitForAsync(string to, string subject, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            var match = _sent.LastOrDefault(m => string.Equals(m.To, to, StringComparison.OrdinalIgnoreCase) && m.Subject == subject);
            if (match is not null)
            {
                return match;
            }
            await Task.Delay(50);
        }
        throw new TimeoutException($"No email '{subject}' was sent to {to}.");
    }

    public static Uri ExtractLink(EmailMessage message)
    {
        var match = LinkPattern().Match(message.HtmlMessage);
        if (!match.Success)
        {
            throw new InvalidOperationException("The email has no link.");
        }
        return new Uri(WebUtility.HtmlDecode(match.Groups["url"].Value));
    }

    public static string ExtractResetCode(EmailMessage message)
    {
        var match = CodePattern().Match(message.HtmlMessage);
        if (!match.Success)
        {
            throw new InvalidOperationException("The email has no reset code.");
        }
        return match.Value;
    }

    public static IReadOnlyDictionary<string, string> QueryOf(Uri link)
        => link.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair.Length > 1 ? pair[1] : string.Empty));

    [GeneratedRegex("href=\"(?<url>https?://[^\"]+)\"")]
    private static partial Regex LinkPattern();

    [GeneratedRegex("(?<![0-9])[0-9]{6}(?![0-9])")]
    private static partial Regex CodePattern();
}
