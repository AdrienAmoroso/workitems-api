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

    /// <summary>
    /// Serialises the event to JSON and sends it to the configured Service Bus topic.
    /// Sets <c>Subject</c> to the CLR type name so the consumer can route by message header
    /// without deserialising the payload first.
    /// </summary>
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(@event))
        {
            Subject     = typeof(T).Name,
            ContentType = "application/json"
        };
        await _sender.SendMessageAsync(message, cancellationToken);
    }

    /// <summary>
    /// Disposes the sender before the client — the sender holds an AMQP link that must
    /// be closed before tearing down the underlying connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
