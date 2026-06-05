namespace WorkItems.Api.Services;

using WorkItems.Api.Contracts.WorkItems;
using WorkItems.Api.Data;
using WorkItems.Api.Domain;
using WorkItems.Api.Hubs;
using WorkItems.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class WorkItemService : IWorkItemService
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<WorkItemsHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WorkItemService> _logger;

    public WorkItemService(
        AppDbContext dbContext,
        IHubContext<WorkItemsHub> hubContext,
        IEventPublisher eventPublisher,
        ILogger<WorkItemService> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<WorkItemResponse> GetByIdAsync(Guid id)
    {
        var workItem = await _dbContext.WorkItems.FirstOrDefaultAsync(x => x.Id == id);

        if (workItem is null)
            throw new NotFoundException($"Work item with ID {id} not found.");

        return WorkItemResponse.FromEntity(workItem);
    }

    public async Task<PaginatedResult<WorkItemResponse>> GetAllAsync(
        int page = 1,
        int pageSize = 10,
        WorkItemStatus? status = null,
        WorkItemPriority? priority = null,
        string sortBy = "createdAt",
        string sortDir = "desc")
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _dbContext.WorkItems.AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(x => x.Priority == priority.Value);

        var totalCount = await query.CountAsync();
        query = ApplySorting(query, sortBy, sortDir);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => WorkItemResponse.FromEntity(x))
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResult<WorkItemResponse>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items
        };
    }

    public async Task<WorkItemResponse> CreateAsync(CreateWorkItemRequest request)
    {
        var workItem = new WorkItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = WorkItemStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.WorkItems.Add(workItem);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Work item created: {WorkItemId} — \"{Title}\"", workItem.Id, workItem.Title);

        var response = WorkItemResponse.FromEntity(workItem);

        await Task.WhenAll(
            _hubContext.Clients.All.SendAsync(WorkItemsHubEvents.WorkItemCreated, response),
            _eventPublisher.PublishAsync(new WorkItemCreatedEvent(
                workItem.Id,
                workItem.Title,
                workItem.Priority.ToString(),
                workItem.Status.ToString(),
                DateTimeOffset.UtcNow)));

        return response;
    }

    public async Task<WorkItemResponse> UpdateAsync(Guid id, UpdateWorkItemRequest request)
    {
        var workItem = await _dbContext.WorkItems.FirstOrDefaultAsync(x => x.Id == id);

        if (workItem is null)
            throw new NotFoundException($"Work item with ID {id} not found.");

        var oldStatus = workItem.Status;

        workItem.Title = request.Title;
        workItem.Description = request.Description;
        workItem.Status = request.Status;
        workItem.Priority = request.Priority;
        workItem.UpdatedAt = DateTime.UtcNow;

        _dbContext.WorkItems.Update(workItem);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Work item updated: {WorkItemId} — \"{Title}\"", workItem.Id, workItem.Title);

        var response = WorkItemResponse.FromEntity(workItem);

        await Task.WhenAll(
            _hubContext.Clients.All.SendAsync(WorkItemsHubEvents.WorkItemUpdated, response),
            _eventPublisher.PublishAsync(new WorkItemUpdatedEvent(
                workItem.Id,
                workItem.Title,
                oldStatus.ToString(),
                workItem.Status.ToString(),
                workItem.Priority.ToString(),
                DateTimeOffset.UtcNow)));

        return response;
    }

    public async Task DeleteAsync(Guid id)
    {
        var workItem = await _dbContext.WorkItems.FirstOrDefaultAsync(x => x.Id == id);

        if (workItem is null)
            throw new NotFoundException($"Work item with ID {id} not found.");

        // Capture title before removal — entity is detached after SaveChanges.
        var deletedId = workItem.Id;
        var deletedTitle = workItem.Title;

        _dbContext.WorkItems.Remove(workItem);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Work item deleted: {WorkItemId} — \"{Title}\"", deletedId, deletedTitle);

        await Task.WhenAll(
            _hubContext.Clients.All.SendAsync(WorkItemsHubEvents.WorkItemDeleted, deletedId),
            _eventPublisher.PublishAsync(new WorkItemDeletedEvent(
                deletedId,
                deletedTitle,
                DateTimeOffset.UtcNow)));
    }

    private static IQueryable<WorkItem> ApplySorting(
        IQueryable<WorkItem> query,
        string sortBy,
        string sortDir)
    {
        var isDescending = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLower() switch
        {
            "title" => isDescending
                ? query.OrderByDescending(x => x.Title)
                : query.OrderBy(x => x.Title),
            "status" => isDescending
                ? query.OrderByDescending(x => x.Status)
                : query.OrderBy(x => x.Status),
            "priority" => isDescending
                ? query.OrderByDescending(x => x.Priority)
                : query.OrderBy(x => x.Priority),
            "updatedat" => isDescending
                ? query.OrderByDescending(x => x.UpdatedAt)
                : query.OrderBy(x => x.UpdatedAt),
            _ => isDescending
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt)
        };
    }
}
