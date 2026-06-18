namespace WorkItems.Contracts;

/// <summary>
/// Domain event raised when a work item is permanently removed.
/// Carries the title at the point of deletion — the record no longer exists in the
/// database when this event is consumed, so no follow-up query is possible.
/// </summary>
public record WorkItemDeletedEvent(
    Guid WorkItemId,
    string Title,
    DateTimeOffset OccurredAt);
