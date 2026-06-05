namespace WorkItems.Contracts;

/// <summary>
/// Published to Service Bus when a work item is updated.
/// Includes the status transition (OldStatus → NewStatus) for audit trail purposes.
/// </summary>
public record WorkItemUpdatedEvent(
    Guid WorkItemId,
    string Title,
    string OldStatus,
    string NewStatus,
    string Priority,
    DateTimeOffset OccurredAt);
