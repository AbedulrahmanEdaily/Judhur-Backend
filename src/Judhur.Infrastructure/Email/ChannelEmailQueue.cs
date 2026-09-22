using System.Threading.Channels;

using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;

namespace Judhur.Infrastructure.Email;

internal sealed class ChannelEmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct) => _channel.Writer.WriteAsync(message, ct);
    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}