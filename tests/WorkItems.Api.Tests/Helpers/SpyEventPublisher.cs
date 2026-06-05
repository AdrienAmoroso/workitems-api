namespace WorkItems.Api.Tests.Helpers;

using WorkItems.Api.Services;

/// <summary>
/// Test double for IEventPublisher. Captures published events in-memory so
/// integration tests can assert that the correct events were published after mutations.
/// </summary>
public class SpyEventPublisher : IEventPublisher
{
    private readonly List<object> _events = new();

    public IReadOnlyList<object> PublishedEvents => _events.AsReadOnly();

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        _events.Add(@event);
        return Task.CompletedTask;
    }

    public void Clear() => _events.Clear();
}
