using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;

using Microsoft.Extensions.Logging;

namespace Judhur.Infrastructure.Service;

public class EmailSender(ILogger<EmailSender> logger) : IEmailSender
{
    private readonly ILogger<EmailSender> _logger = logger;

    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var at = message.Email.IndexOf('@');
        var maskedEmail = at > 1
            ? message.Email[0] + new string('*', at - 2) + message.Email[at - 1] + message.Email[at..]
            : "*****";

        _logger.LogInformation("[Email] To: {Email} | Message: {Subject}", maskedEmail, message.Subject);

        // Simulated email send
        await Task.CompletedTask;
    }
}