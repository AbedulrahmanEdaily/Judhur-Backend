using Judhur.Application.Common.Models;

namespace Judhur.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(EmailMessage message,CancellationToken cancellationToken);
}