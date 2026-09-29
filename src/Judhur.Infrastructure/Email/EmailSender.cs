using System.Net;
using System.Net.Mail;
using System.Text;

using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Judhur.Infrastructure.Email;

internal sealed class EmailSender(ILogger<EmailSender> logger, IConfiguration configuration) : IEmailSender
{
    private readonly ILogger<EmailSender> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var fromAddress = new MailAddress("dylyb7883@gmail.com", "Judhur");
        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential("dylyb7883@gmail.com", _configuration["EmailPassword"])
        };
        using var mailMessage = new MailMessage
        {
            From = fromAddress,
            Subject = message.Subject,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        mailMessage.To.Add(message.To);
        var htmlView = AlternateView.CreateAlternateViewFromString(message.HtmlMessage, Encoding.UTF8, "text/html");
        if (message.HtmlMessage.Contains($"cid:{EmailTemplates.LogoContentId}"))
        {
            var logo = new LinkedResource(EmailTemplates.OpenLogo(), "image/png")
            {
                ContentId = EmailTemplates.LogoContentId
            };
            htmlView.LinkedResources.Add(logo);
        }
        mailMessage.AlternateViews.Add(htmlView);

        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}