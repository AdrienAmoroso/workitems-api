namespace DotnetPortfolioApi.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DotnetPortfolioApi.Api.Contracts.Auth;
using DotnetPortfolioApi.Api.Contracts.WorkItems;
using DotnetPortfolioApi.Api.Data;
using DotnetPortfolioApi.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class WorkItemsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly string DatabaseName = "TestDatabase_WorkItems_" + Guid.NewGuid();
    private const string TestSecretKey = "TestSecretKeyForJWTThatIsAtLeast32CharactersLong123456";
    private const string TestIssuer = "DotNetPortfolioApi";
    private const string TestAudience = "DotNetPortfolioApiUsers";

    public WorkItemsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = TestSecretKey,
                    ["Jwt:Issuer"] = TestIssuer,
                    ["Jwt:Audience"] = TestAudience,
                    ["Jwt:ExpirationHours"] = "24"
                }!);
            });
            
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                });

                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = TestIssuer,
                        ValidAudience = TestAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
            });
        });
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
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<WorkItemResponse>>();
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
        var createdItem = await createResponse.Content.ReadFromJsonAsync<WorkItemResponse>();

        // Act
        var response = await client.GetAsync($"/api/work-items/{createdItem!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WorkItemResponse>();
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
        var result = await response.Content.ReadFromJsonAsync<WorkItemResponse>();
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
        var createdItem = await createResponse.Content.ReadFromJsonAsync<WorkItemResponse>();

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
        var result = await response.Content.ReadFromJsonAsync<WorkItemResponse>();
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
        var token = await RegisterAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWorkItemRequest
        {
            Title = "To Delete",
            Description = "Will be deleted",
            Priority = Domain.WorkItemPriority.Low
        };

        var createResponse = await client.PostAsJsonAsync("/api/work-items", createRequest);
        var createdItem = await createResponse.Content.ReadFromJsonAsync<WorkItemResponse>();

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
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<WorkItemResponse>>();
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
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<WorkItemResponse>>();
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
