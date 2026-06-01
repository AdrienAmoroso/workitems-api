namespace WorkItems.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using WorkItems.Api.Contracts.WorkItems;
using WorkItems.Api.Domain;
using WorkItems.Api.Hubs;
using WorkItems.Api.Tests.Helpers;

/// <summary>
/// Verifies that WorkItemsHub enforces authentication and broadcasts the correct
/// SignalR events when work items are created, updated, or deleted via the REST API.
/// Uses LongPolling transport — WebSockets require a real TCP listener which the
/// TestServer does not provide, but LongPolling uses standard HTTP and works fine.
/// </summary>
public class SignalRHubIntegrationTests : IClassFixture<WorkItemsWebApplicationFactory>
{
    private readonly WorkItemsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SignalRHubIntegrationTests(WorkItemsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Connect_WithoutToken_IsRejected()
    {
        var connection = BuildConnection(token: null);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => connection.StartAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task Connect_WithValidToken_Succeeds()
    {
        var token      = JwtTestHelper.CreateToken("Member");
        var connection = BuildConnection(token);

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.StopAsync();
    }

    // ── Broadcasts ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_BroadcastsWorkItemCreatedEvent()
    {
        var token      = JwtTestHelper.CreateToken("Admin");
        var connection = BuildConnection(token);

        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<JsonElement>(WorkItemsHubEvents.WorkItemCreated, payload => tcs.TrySetResult(payload));

        await connection.StartAsync();

        var client = CreateAuthenticatedClient(token);
        var created = await PostWorkItemAsync(client, "SignalR create test");

        var received = await WaitForEventAsync(tcs);
        Assert.Equal(created.Id.ToString(), received.GetProperty("id").GetString());
        Assert.Equal("SignalR create test", received.GetProperty("title").GetString());

        await connection.StopAsync();
    }

    [Fact]
    public async Task Update_BroadcastsWorkItemUpdatedEvent()
    {
        var token      = JwtTestHelper.CreateToken("Admin");
        var connection = BuildConnection(token);

        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<JsonElement>(WorkItemsHubEvents.WorkItemUpdated, payload => tcs.TrySetResult(payload));

        await connection.StartAsync();

        var client = CreateAuthenticatedClient(token);
        var created = await PostWorkItemAsync(client, "SignalR update test");

        await client.PutAsJsonAsync($"/api/work-items/{created.Id}", new UpdateWorkItemRequest
        {
            Title    = "SignalR update test — edited",
            Status   = WorkItemStatus.InProgress,
            Priority = WorkItemPriority.High
        });

        var received = await WaitForEventAsync(tcs);
        Assert.Equal(created.Id.ToString(), received.GetProperty("id").GetString());
        Assert.Equal("SignalR update test — edited", received.GetProperty("title").GetString());

        await connection.StopAsync();
    }

    [Fact]
    public async Task Delete_BroadcastsWorkItemDeletedEvent()
    {
        var token      = JwtTestHelper.CreateToken("Admin");
        var connection = BuildConnection(token);

        // WorkItemDeleted sends the Guid directly, not a JSON object
        var tcs = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<Guid>(WorkItemsHubEvents.WorkItemDeleted, id => tcs.TrySetResult(id));

        await connection.StartAsync();

        var client = CreateAuthenticatedClient(token);
        var created = await PostWorkItemAsync(client, "SignalR delete test");

        await client.DeleteAsync($"/api/work-items/{created.Id}");

        var deletedId = await WaitForEventAsync(tcs);
        Assert.Equal(created.Id, deletedId);

        await connection.StopAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HubConnection BuildConnection(string? token) =>
        new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/workitems", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports               = HttpTransportType.LongPolling;
                if (token is not null)
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<WorkItemResponse> PostWorkItemAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/work-items", new CreateWorkItemRequest
        {
            Title    = title,
            Priority = WorkItemPriority.Medium
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions))!;
    }

    private static async Task<T> WaitForEventAsync<T>(TaskCompletionSource<T> tcs)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcs.TrySetCanceled());
        return await tcs.Task;
    }
}
