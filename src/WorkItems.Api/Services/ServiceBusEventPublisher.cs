namespace WorkItems.Api.Services;

using Azure.Messaging.ServiceBus;

/// <summary>
/// Sends domain events to an Azure Service Bus topic as JSON messages.
/// The message Subject is set to the event type name so the consumer can route without deserializing the body.
/// </summary>
public sealed class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusEventPublisher(IConfiguration configuration)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"]!;
        var topicName = configuration["ServiceBus:TopicName"] ?? "workitems-events";
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(@event))
        {
            Subject     = typeof(T).Name,
            ContentType = "application/json"
        };
        await _sender.SendMessageAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
