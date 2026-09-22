using Judhur.Application.Common.Models;

namespace Judhur.Application.Common.Interfaces;

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}