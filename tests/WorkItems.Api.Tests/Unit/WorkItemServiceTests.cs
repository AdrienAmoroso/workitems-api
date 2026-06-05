namespace WorkItems.Api.Tests.Unit;

using WorkItems.Api.Contracts.WorkItems;
using WorkItems.Api.Data;
using WorkItems.Api.Domain;
using WorkItems.Api.Hubs;
using WorkItems.Api.Services;
using WorkItems.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

public class WorkItemServiceTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ILogger<WorkItemService> CreateMockLogger() =>
        new Mock<ILogger<WorkItemService>>().Object;

    private static IEventPublisher CreateMockPublisher() =>
        new Mock<IEventPublisher>().Object;

    private static IHubContext<WorkItemsHub> CreateMockHubContext()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        mockClientProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);
        var mock = new Mock<IHubContext<WorkItemsHub>>();
        mock.Setup(h => h.Clients).Returns(mockClients.Object);
        return mock.Object;
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsWorkItem()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());

        var workItem = new WorkItem
        {
            Id = Guid.NewGuid(),
            Title = "Test Item",
            Description = "Test Description",
            Status = WorkItemStatus.Todo,
            Priority = WorkItemPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.WorkItems.Add(workItem);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByIdAsync(workItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workItem.Id, result.Id);
        Assert.Equal(workItem.Title, result.Title);
        Assert.Equal(workItem.Description, result.Description);
        Assert.Equal(workItem.Status, result.Status);
        Assert.Equal(workItem.Priority, result.Priority);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(nonExistingId));
    }

    [Fact]
    public async Task GetAllAsync_NoFilters_ReturnsAllItems()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());

        var workItems = new[]
        {
            new WorkItem { Title = "Item 1", Priority = WorkItemPriority.High, Status = WorkItemStatus.Todo, CreatedAt = DateTime.UtcNow },
            new WorkItem { Title = "Item 2", Priority = WorkItemPriority.Medium, Status = WorkItemStatus.InProgress, CreatedAt = DateTime.UtcNow },
            new WorkItem { Title = "Item 3", Priority = WorkItemPriority.Low, Status = WorkItemStatus.Done, CreatedAt = DateTime.UtcNow }
        };

        context.WorkItems.AddRange(workItems);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsFilteredItems()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());

        var workItems = new[]
        {
            new WorkItem { Title = "Item 1", Priority = WorkItemPriority.High, Status = WorkItemStatus.Todo, CreatedAt = DateTime.UtcNow },
            new WorkItem { Title = "Item 2", Priority = WorkItemPriority.Medium, Status = WorkItemStatus.InProgress, CreatedAt = DateTime.UtcNow },
            new WorkItem { Title = "Item 3", Priority = WorkItemPriority.Low, Status = WorkItemStatus.Todo, CreatedAt = DateTime.UtcNow }
        };

        context.WorkItems.AddRange(workItems);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync(status: WorkItemStatus.Todo);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal(WorkItemStatus.Todo, item.Status));
    }

    [Fact]
    public async Task GetAllAsync_WithPriorityFilter_ReturnsFilteredItems()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());

        var workItems = new[]
        {
            new WorkItem { Title = "Item 1", Priority = WorkItemPriority.High, Status = WorkItemStatus.Todo, CreatedAt = DateTime.UtcNow },
            new WorkItem { Title = "Item 2", Priority = WorkItemPriority.High, Status = WorkItemStatus.InProgress, CreatedAt = DateTime.UtcNow },
            new WorkItem { Title = "Item 3", Priority = WorkItemPriority.Low, Status = WorkItemStatus.Todo, CreatedAt = DateTime.UtcNow }
        };

        context.WorkItems.AddRange(workItems);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync(priority: WorkItemPriority.High);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal(WorkItemPriority.High, item.Priority));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());

        for (int i = 1; i <= 15; i++)
        {
            context.WorkItems.Add(new WorkItem
            {
                Title = $"Item {i}",
                Priority = WorkItemPriority.Medium,
                Status = WorkItemStatus.Todo,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var page1 = await service.GetAllAsync(page: 1, pageSize: 5);
        var page2 = await service.GetAllAsync(page: 2, pageSize: 5);

        // Assert
        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(5, page1.Items.Count);
        Assert.Equal(3, page1.TotalPages);
        Assert.True(page1.HasNextPage);
        Assert.False(page1.HasPreviousPage);

        Assert.Equal(15, page2.TotalCount);
        Assert.Equal(5, page2.Items.Count);
        Assert.True(page2.HasNextPage);
        Assert.True(page2.HasPreviousPage);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesWorkItem()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());

        var request = new CreateWorkItemRequest
        {
            Title = "New Task",
            Description = "Task description",
            Priority = WorkItemPriority.High
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.Priority, result.Priority);
        Assert.Equal(WorkItemStatus.Todo, result.Status);

        var savedItem = await context.WorkItems.FindAsync(result.Id);
        Assert.NotNull(savedItem);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesWorkItem()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());

        var workItem = new WorkItem
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            Status = WorkItemStatus.Todo,
            Priority = WorkItemPriority.Low,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.WorkItems.Add(workItem);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateWorkItemRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Status = WorkItemStatus.InProgress,
            Priority = WorkItemPriority.High
        };

        // Act
        var result = await service.UpdateAsync(workItem.Id, updateRequest);

        // Assert
        Assert.Equal(workItem.Id, result.Id);
        Assert.Equal(updateRequest.Title, result.Title);
        Assert.Equal(updateRequest.Description, result.Description);
        Assert.Equal(updateRequest.Status, result.Status);
        Assert.Equal(updateRequest.Priority, result.Priority);

        var updatedItem = await context.WorkItems.FindAsync(workItem.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal(updateRequest.Title, updatedItem.Title);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());
        var nonExistingId = Guid.NewGuid();

        var updateRequest = new UpdateWorkItemRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Status = WorkItemStatus.InProgress,
            Priority = WorkItemPriority.High
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(nonExistingId, updateRequest));
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesWorkItem()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());

        var workItem = new WorkItem
        {
            Id = Guid.NewGuid(),
            Title = "To Delete",
            Priority = WorkItemPriority.Medium,
            Status = WorkItemStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.WorkItems.Add(workItem);
        await context.SaveChangesAsync();

        // Act
        await service.DeleteAsync(workItem.Id);

        // Assert
        var deletedItem = await context.WorkItems.FindAsync(workItem.Id);
        Assert.Null(deletedItem);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(nonExistingId));
    }

    [Fact]
    public async Task GetAllAsync_SortByTitle_ReturnsSortedItems()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new WorkItemService(context, CreateMockHubContext(), CreateMockPublisher(), CreateMockLogger());

        var workItems = new[]
        {
            new WorkItem { Title = "Charlie", Priority = WorkItemPriority.Medium, Status = WorkItemStatus.Todo, CreatedAt = DateTime.UtcNow },
            new WorkItem { Title = "Alpha", Priority = WorkItemPriority.Medium, Status = WorkItemStatus.Todo, CreatedAt = DateTime.UtcNow },
            new WorkItem { Title = "Bravo", Priority = WorkItemPriority.Medium, Status = WorkItemStatus.Todo, CreatedAt = DateTime.UtcNow }
        };

        context.WorkItems.AddRange(workItems);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync(sortBy: "title", sortDir: "asc");

        // Assert
        Assert.Equal("Alpha", result.Items[0].Title);
        Assert.Equal("Bravo", result.Items[1].Title);
        Assert.Equal("Charlie", result.Items[2].Title);
    }
}
