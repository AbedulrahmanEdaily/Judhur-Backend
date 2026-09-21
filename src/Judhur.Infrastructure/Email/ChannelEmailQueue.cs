using System.Threading.Channels;

using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;

namespace Judhur.Infrastructure.Email;

internal sealed class ChannelEmailQueue : IEmailQueue
{
    private readonly Channel<EmailConfirmationRequest> _channel = Channel.CreateUnbounded<EmailConfirmationRequest>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    public ValueTask EnqueueAsync(EmailConfirmationRequest request, CancellationToken ct) => _channel.Writer.WriteAsync(request, ct);
    public IAsyncEnumerable<EmailConfirmationRequest> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}