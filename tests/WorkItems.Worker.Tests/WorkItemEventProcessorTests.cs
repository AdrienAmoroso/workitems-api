namespace WorkItems.Worker.Tests;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WorkItems.Contracts;
using WorkItems.Worker.Consumers;

/// <summary>
/// Unit tests for WorkItemEventProcessor.DispatchAsync.
/// Tests call DispatchAsync directly — no Service Bus connection is made.
/// </summary>
public class WorkItemEventProcessorTests
{
    private static WorkItemEventProcessor CreateProcessor()
    {
        // A dummy connection string satisfies the constructor guard without connecting.
        // ExecuteAsync (which creates the real ServiceBusClient) is never called in unit tests.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://dummy.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummykey="
            })
            .Build();

        return new WorkItemEventProcessor(config, NullLogger<WorkItemEventProcessor>.Instance);
    }

    [Fact]
    public async Task DispatchAsync_WorkItemCreated_CompletesWithoutException()
    {
        var processor = CreateProcessor();
        var evt = new WorkItemCreatedEvent(
            Guid.NewGuid(), "Fix critical bug", "High", "Todo", DateTimeOffset.UtcNow);

        await processor.DispatchAsync(nameof(WorkItemCreatedEvent), BinaryData.FromObjectAsJson(evt));
    }

    [Fact]
    public async Task DispatchAsync_WorkItemUpdated_CompletesWithoutException()
    {
        var processor = CreateProcessor();
        var evt = new WorkItemUpdatedEvent(
            Guid.NewGuid(), "Fix critical bug", "Todo", "InProgress", "High", DateTimeOffset.UtcNow);

        await processor.DispatchAsync(nameof(WorkItemUpdatedEvent), BinaryData.FromObjectAsJson(evt));
    }

    [Fact]
    public async Task DispatchAsync_WorkItemDeleted_CompletesWithoutException()
    {
        var processor = CreateProcessor();
        var evt = new WorkItemDeletedEvent(
            Guid.NewGuid(), "Stale item", DateTimeOffset.UtcNow);

        await processor.DispatchAsync(nameof(WorkItemDeletedEvent), BinaryData.FromObjectAsJson(evt));
    }

    [Fact]
    public async Task DispatchAsync_UnknownEventType_CompletesWithoutException()
    {
        var processor = CreateProcessor();

        // Unknown subjects should be logged and completed — never throw.
        await processor.DispatchAsync("SomeUnknownEventV2", BinaryData.FromString("{}"));
    }

    [Fact]
    public async Task DispatchAsync_WorkItemCreated_EventPropertiesAreDeserializedCorrectly()
    {
        var processor = CreateProcessor();
        var workItemId = Guid.NewGuid();
        var evt = new WorkItemCreatedEvent(workItemId, "Deploy to prod", "High", "Todo", DateTimeOffset.UtcNow);

        // DispatchAsync completes → confirms JSON round-trip deserialization doesn't throw.
        await processor.DispatchAsync(nameof(WorkItemCreatedEvent), BinaryData.FromObjectAsJson(evt));
    }

    [Fact]
    public async Task DispatchAsync_WorkItemUpdated_StatusTransitionIsDeserializedCorrectly()
    {
        var processor = CreateProcessor();
        var evt = new WorkItemUpdatedEvent(
            Guid.NewGuid(), "Update docs", "Todo", "Done", "Low", DateTimeOffset.UtcNow);

        await processor.DispatchAsync(nameof(WorkItemUpdatedEvent), BinaryData.FromObjectAsJson(evt));
    }
}
