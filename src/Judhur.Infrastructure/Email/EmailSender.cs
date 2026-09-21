using System.Net;
using System.Net.Mail;

using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;

using Microsoft.Extensions.Logging;

namespace Judhur.Infrastructure.Email;

internal sealed class EmailSender(ILogger<EmailSender> logger) : IEmailSender
{
    private readonly ILogger<EmailSender> _logger = logger;

    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var fromAddress = new MailAddress("dylyb7883@gmail.com", "Judhur");
        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential("dylyb7883@gmail.com", "enlb vrpd nadq xmlk")
        };
        var mailMessage = new MailMessage
        {
            From = fromAddress,
            Subject = message.Subject,
            Body = message.HtmlMessage,
            IsBodyHtml = true
        };
        mailMessage.To.Add(message.To);
        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}