namespace WorkItems.Contracts;

/// <summary>
/// Published to Service Bus when a work item is permanently deleted.
/// </summary>
public record WorkItemDeletedEvent(
    Guid WorkItemId,
    string Title,
    DateTimeOffset OccurredAt);
