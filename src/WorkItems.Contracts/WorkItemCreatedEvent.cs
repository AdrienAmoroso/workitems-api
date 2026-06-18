namespace WorkItems.Contracts;

/// <summary>
/// Domain event raised when a new work item is persisted.
/// Crosses the Service Bus boundary as a JSON message; all subscribers receive it
/// via their own subscription — the producer has no knowledge of downstream handlers.
/// </summary>
public record WorkItemCreatedEvent(
    Guid WorkItemId,
    string Title,
    string Priority,
    string Status,
    DateTimeOffset OccurredAt);
