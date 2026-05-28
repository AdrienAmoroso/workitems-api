namespace WorkItems.Api.Hubs;

/// <summary>
/// String constants for SignalR event method names broadcast from the server.
/// Used in WorkItemService (SendAsync) and documented for the Angular client.
/// </summary>
public static class WorkItemsHubEvents
{
    public const string WorkItemCreated = "WorkItemCreated";
    public const string WorkItemUpdated = "WorkItemUpdated";
    public const string WorkItemDeleted = "WorkItemDeleted";
}
