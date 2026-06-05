namespace WorkItems.Contracts;

/// <summary>
/// Published to Service Bus when a work item is created.
/// Consumed by WorkItems.Worker to write a structured audit log entry.
/// </summary>
public record WorkItemCreatedEvent(
    Guid WorkItemId,
    string Title,
    string Priority,
    string Status,
    DateTimeOffset OccurredAt);
