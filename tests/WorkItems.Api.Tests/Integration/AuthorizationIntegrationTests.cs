namespace WorkItems.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkItems.Api.Contracts.WorkItems;
using WorkItems.Api.Domain;
using WorkItems.Api.Tests.Helpers;

/// <summary>
/// Verifies that authorization policies (CanManageWorkItems / CanDeleteWorkItems)
/// grant or deny access correctly based on the caller's role.
/// Each test uses a JWT minted by JwtTestHelper — no DB registration needed.
/// </summary>
public class AuthorizationIntegrationTests : IClassFixture<WorkItemsWebApplicationFactory>
{
    private readonly WorkItemsWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AuthorizationIntegrationTests(WorkItemsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_AsAdmin_ReturnsNoContent()
    {
        // Arrange — create an item using an Admin token
        var client     = _factory.CreateClient();
        var adminToken = JwtTestHelper.CreateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var created = await CreateWorkItemAsync(client);

        // Act
        var response = await client.DeleteAsync($"/api/work-items/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsMember_ReturnsForbidden()
    {
        // Arrange — create an item as Admin, then try to delete as Member
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Admin"));
        var created = await CreateWorkItemAsync(adminClient);

        var memberClient = _factory.CreateClient();
        memberClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Member"));

        // Act
        var response = await memberClient.DeleteAsync($"/api/work-items/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsViewer_ReturnsForbidden()
    {
        // Arrange
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Admin"));
        var created = await CreateWorkItemAsync(adminClient);

        var viewerClient = _factory.CreateClient();
        viewerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Viewer"));

        // Act
        var response = await viewerClient.DeleteAsync($"/api/work-items/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Unauthenticated_ReturnsUnauthorized()
    {
        var client   = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/work-items/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST (create) ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_AsMember_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Member"));

        var response = await client.PostAsJsonAsync("/api/work-items", new CreateWorkItemRequest
        {
            Title    = "Member task",
            Priority = WorkItemPriority.Medium
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsViewer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Viewer"));

        var response = await client.PostAsJsonAsync("/api/work-items", new CreateWorkItemRequest
        {
            Title    = "Viewer task",
            Priority = WorkItemPriority.Low
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── PUT (update) ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_AsMember_ReturnsOk()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Admin"));
        var created = await CreateWorkItemAsync(adminClient);

        var memberClient = _factory.CreateClient();
        memberClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Member"));

        var response = await memberClient.PutAsJsonAsync($"/api/work-items/{created.Id}",
            new UpdateWorkItemRequest
            {
                Title    = "Updated by Member",
                Status   = WorkItemStatus.InProgress,
                Priority = WorkItemPriority.Medium
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Admin"));
        var created = await CreateWorkItemAsync(client);

        var response = await client.PutAsJsonAsync($"/api/work-items/{created.Id}",
            new UpdateWorkItemRequest
            {
                Title    = "Updated by Admin",
                Status   = WorkItemStatus.Done,
                Priority = WorkItemPriority.High
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsViewer_ReturnsForbidden()
    {
        // Arrange — create item as Admin
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Admin"));
        var created = await CreateWorkItemAsync(adminClient);

        // Act — try to update as Viewer
        var viewerClient = _factory.CreateClient();
        viewerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken("Viewer"));

        var response = await viewerClient.PutAsJsonAsync($"/api/work-items/{created.Id}",
            new UpdateWorkItemRequest
            {
                Title    = "Should fail",
                Status   = WorkItemStatus.InProgress,
                Priority = WorkItemPriority.High
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static async Task<WorkItemResponse> CreateWorkItemAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/work-items", new CreateWorkItemRequest
        {
            Title       = "Seeded for auth test",
            Description = "Created by test setup",
            Priority    = WorkItemPriority.Low
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkItemResponse>(JsonOptions))!;
    }
}
