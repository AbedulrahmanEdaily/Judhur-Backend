using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Judhur.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    public const string Route = "/hubs/chat";
}
