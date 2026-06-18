namespace WorkItems.Contracts;

/// <summary>
/// Domain event raised when a work item is modified.
/// Carries both <see cref="OldStatus"/> and <see cref="NewStatus"/> so consumers
/// can react to status transitions without querying the database.
/// </summary>
public record WorkItemUpdatedEvent(
    Guid WorkItemId,
    string Title,
    string OldStatus,
    string NewStatus,
    string Priority,
    DateTimeOffset OccurredAt);
