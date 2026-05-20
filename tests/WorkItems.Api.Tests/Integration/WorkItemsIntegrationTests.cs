namespace WorkItems.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkItems.Api.Contracts.Auth;
using WorkItems.Api.Contracts.WorkItems;
using WorkItems.Api.Services;
using WorkItems.Api.Tests.Helpers;

public class WorkItemsIntegrationTests : IClassFixture<WorkItemsWebApplicationFactory>
{
    private readonly WorkItemsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public WorkItemsIntegrationTests(WorkItemsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllWorkItems_NoAuth_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/work-items");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<WorkItemResponse>>(JsonOptions);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task GetWorkItemById_ExistingId_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWorkItemRequest
        {
            Title = "Test Item",
            Description = "Test Description",
            Priority = Domain.WorkItemPriority.Medium
        };

        var createResponse = await client.PostAsJsonAsync("/api/work-items", createRequest);
        var createdItem = await createResponse.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);

        // Act
        var response = await client.GetAsync($"/api/work-items/{createdItem!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(createdItem.Id, result.Id);
        Assert.Equal(createRequest.Title, result.Title);
    }

    [Fact]
    public async Task GetWorkItemById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/work-items/{nonExistingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkItem_WithAuth_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateWorkItemRequest
        {
            Title = "New Task",
            Description = "Task description",
            Priority = Domain.WorkItemPriority.High
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/work-items", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.Priority, result.Priority);
    }

    [Fact]
    public async Task CreateWorkItem_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new CreateWorkItemRequest
        {
            Title = "New Task",
            Description = "Task description",
            Priority = Domain.WorkItemPriority.High
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/work-items", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkItem_WithAuth_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWorkItemRequest
        {
            Title = "Original Title",
            Description = "Original Description",
            Priority = Domain.WorkItemPriority.Low
        };

        var createResponse = await client.PostAsJsonAsync("/api/work-items", createRequest);
        var createdItem = await createResponse.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);

        var updateRequest = new UpdateWorkItemRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Status = Domain.WorkItemStatus.InProgress,
            Priority = Domain.WorkItemPriority.High
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/work-items/{createdItem!.Id}", updateRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(updateRequest.Title, result.Title);
        Assert.Equal(updateRequest.Description, result.Description);
        Assert.Equal(updateRequest.Status, result.Status);
        Assert.Equal(updateRequest.Priority, result.Priority);
    }

    [Fact]
    public async Task UpdateWorkItem_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        var updateRequest = new UpdateWorkItemRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Status = Domain.WorkItemStatus.InProgress,
            Priority = Domain.WorkItemPriority.High
        };

        var randomId = Guid.NewGuid();

        // Act
        var response = await client.PutAsJsonAsync($"/api/work-items/{randomId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkItem_WithAuth_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        // DELETE requires Admin role (CanDeleteWorkItems policy) — use a direct token
        var adminToken = JwtTestHelper.CreateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createRequest = new CreateWorkItemRequest
        {
            Title = "To Delete",
            Description = "Will be deleted",
            Priority = Domain.WorkItemPriority.Low
        };

        var createResponse = await client.PostAsJsonAsync("/api/work-items", createRequest);
        var createdItem = await createResponse.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions);

        // Act
        var response = await client.DeleteAsync($"/api/work-items/{createdItem!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's deleted
        var getResponse = await client.GetAsync($"/api/work-items/{createdItem.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkItem_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var randomId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/work-items/{randomId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllWorkItems_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create multiple work items
        var requests = new[]
        {
            new CreateWorkItemRequest { Title = "High Priority Task", Priority = Domain.WorkItemPriority.High },
            new CreateWorkItemRequest { Title = "Medium Priority Task", Priority = Domain.WorkItemPriority.Medium },
            new CreateWorkItemRequest { Title = "Low Priority Task", Priority = Domain.WorkItemPriority.Low }
        };

        foreach (var request in requests)
        {
            await client.PostAsJsonAsync("/api/work-items", request);
        }

        // Act - Filter by high priority
        var response = await client.GetAsync("/api/work-items?priority=2"); // High = 2

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<WorkItemResponse>>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.Items.Count >= 1);
        Assert.All(result.Items, item => Assert.Equal(Domain.WorkItemPriority.High, item.Priority));
    }

    [Fact]
    public async Task GetAllWorkItems_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create 5 work items
        for (int i = 1; i <= 5; i++)
        {
            await client.PostAsJsonAsync("/api/work-items", new CreateWorkItemRequest
            {
                Title = $"Task {i}",
                Priority = Domain.WorkItemPriority.Medium
            });
        }

        // Act
        var response = await client.GetAsync("/api/work-items?page=1&pageSize=3");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<WorkItemResponse>>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 5);
        Assert.True(result.Items.Count <= 3);
        Assert.Equal(1, result.Page);
    }

    private async Task<string> RegisterAndGetTokenAsync(HttpClient client)
    {
        var registerRequest = new RegisterRequest
        {
            Username = "testuser_" + Guid.NewGuid().ToString("N")[..8],
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "TestPassword123!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }

}

