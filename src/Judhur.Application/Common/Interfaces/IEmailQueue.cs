using Judhur.Application.Common.Models;

namespace Judhur.Application.Common.Interfaces;

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailConfirmationRequest request, CancellationToken cancellationToken = default);
}