namespace WorkItems.Api.Services;

/// <summary>
/// Publishes domain events to an external message broker.
/// The API does not know (or care) who consumes these events — that's the point.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}
