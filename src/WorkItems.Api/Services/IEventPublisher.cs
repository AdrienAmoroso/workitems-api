namespace WorkItems.Api.Services;

/// <summary>
/// Publishes domain events to an external message broker.
/// The API does not know (or care) who consumes these events — that's the point.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a typed domain event to the configured message broker.
    /// Callers are not aware of the underlying transport — Service Bus in production,
    /// no-op locally (see <see cref="NullEventPublisher"/>).
    /// </summary>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}
