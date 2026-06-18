namespace WorkItems.Api.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR hub that broadcasts work item mutations to all connected clients in real time.
/// Requires an authenticated JWT; the token is sourced from the <c>access_token</c> query
/// parameter because the WebSocket protocol does not support custom HTTP headers after
/// the initial handshake (see JWT <c>OnMessageReceived</c> configuration in Program.cs).
/// </summary>
[Authorize]
public class WorkItemsHub : Hub
{
    // No client-callable methods — this hub is push-only.
    // The server sends events via IHubContext<WorkItemsHub> in WorkItemService.
}
