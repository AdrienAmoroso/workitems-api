namespace WorkItems.Api.Services;

/// <summary>
/// No-op publisher used when ServiceBus:ConnectionString is not configured (local dev without Service Bus).
/// Logs a warning so the gap is visible in dev logs without crashing the API.
/// </summary>
public sealed class NullEventPublisher : IEventPublisher
{
    private readonly ILogger<NullEventPublisher> _logger;

    public NullEventPublisher(ILogger<NullEventPublisher> logger) => _logger = logger;

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogWarning(
            "ServiceBus not configured — {EventType} event discarded. Set ServiceBus:ConnectionString to enable event publishing.",
            typeof(T).Name);
        return Task.CompletedTask;
    }
}
