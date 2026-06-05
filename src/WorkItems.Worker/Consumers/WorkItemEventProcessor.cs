namespace WorkItems.Worker.Consumers;

using Azure.Messaging.ServiceBus;
using WorkItems.Contracts;

/// <summary>
/// Subscribes to the workitems-events Service Bus topic and processes domain events.
/// Current behaviour: write a structured audit log entry for each event type.
/// Extend DispatchAsync to add notifications, search index updates, webhooks, etc.
/// </summary>
public class WorkItemEventProcessor : BackgroundService
{
    private readonly string _connectionString;
    private readonly string _topicName;
    private readonly string _subscriptionName;
    private readonly ILogger<WorkItemEventProcessor> _logger;

    public WorkItemEventProcessor(IConfiguration configuration, ILogger<WorkItemEventProcessor> logger)
    {
        _connectionString = configuration["ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException("ServiceBus:ConnectionString is required. Set it via environment variable.");
        _topicName        = configuration["ServiceBus:TopicName"]        ?? "workitems-events";
        _subscriptionName = configuration["ServiceBus:SubscriptionName"] ?? "workitems-worker-sub";
        _logger           = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var client = new ServiceBusClient(_connectionString);
        var processor = client.CreateProcessor(
            _topicName,
            _subscriptionName,
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls  = 1,
                AutoCompleteMessages = false
            });

        processor.ProcessMessageAsync += async args =>
        {
            await DispatchAsync(args.Message.Subject, args.Message.Body, stoppingToken);
            await args.CompleteMessageAsync(args.Message, stoppingToken);
        };

        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(
                args.Exception,
                "Service Bus processing error — entity: {EntityPath}, source: {ErrorSource}",
                args.EntityPath, args.ErrorSource);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);

        _logger.LogInformation(
            "WorkItemEventProcessor started — topic: {Topic}, subscription: {Subscription}",
            _topicName, _subscriptionName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on graceful shutdown — not an error.
        }
        finally
        {
            await processor.StopProcessingAsync();
            _logger.LogInformation("WorkItemEventProcessor stopped.");
        }
    }

    /// <summary>
    /// Routes an incoming message to the correct handler by its Subject header.
    /// Public so unit tests can call this directly without starting the host.
    /// </summary>
    public Task DispatchAsync(string subject, BinaryData body, CancellationToken cancellationToken = default)
    {
        return subject switch
        {
            nameof(WorkItemCreatedEvent) =>
                HandleCreatedAsync(body.ToObjectFromJson<WorkItemCreatedEvent>()!),
            nameof(WorkItemUpdatedEvent) =>
                HandleUpdatedAsync(body.ToObjectFromJson<WorkItemUpdatedEvent>()!),
            nameof(WorkItemDeletedEvent) =>
                HandleDeletedAsync(body.ToObjectFromJson<WorkItemDeletedEvent>()!),
            _ => HandleUnknownAsync(subject)
        };
    }

    private Task HandleCreatedAsync(WorkItemCreatedEvent evt)
    {
        _logger.LogInformation(
            "AUDIT WorkItem {WorkItemId} created — \"{Title}\" [{Priority}] status={Status}",
            evt.WorkItemId, evt.Title, evt.Priority, evt.Status);
        return Task.CompletedTask;
    }

    private Task HandleUpdatedAsync(WorkItemUpdatedEvent evt)
    {
        _logger.LogInformation(
            "AUDIT WorkItem {WorkItemId} updated — \"{Title}\" status {OldStatus} → {NewStatus} [{Priority}]",
            evt.WorkItemId, evt.Title, evt.OldStatus, evt.NewStatus, evt.Priority);
        return Task.CompletedTask;
    }

    private Task HandleDeletedAsync(WorkItemDeletedEvent evt)
    {
        _logger.LogInformation(
            "AUDIT WorkItem {WorkItemId} deleted — \"{Title}\"",
            evt.WorkItemId, evt.Title);
        return Task.CompletedTask;
    }

    private Task HandleUnknownAsync(string subject)
    {
        _logger.LogWarning("Received unknown event type: {Subject} — message will be completed without processing.", subject);
        return Task.CompletedTask;
    }
}
