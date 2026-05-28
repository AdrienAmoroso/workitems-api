namespace WorkItems.Api.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Real-time hub for work item change notifications.
/// Authenticated users only — the server pushes events, clients listen.
/// </summary>
[Authorize]
public class WorkItemsHub : Hub
{
    // No client-callable methods.
    // The server broadcasts via IHubContext<WorkItemsHub> from WorkItemService.
}
