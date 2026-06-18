namespace WorkItems.Api.Services;

using WorkItems.Api.Contracts.WorkItems;
using WorkItems.Api.Data;
using WorkItems.Api.Domain;
using WorkItems.Api.Hubs;
using WorkItems.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Orchestrates all work item CRUD operations, enforcing domain rules and coordinating
/// three concerns after each mutation: persistence (EF Core), real-time push (SignalR),
/// and async event publication (Service Bus via <see cref="IEventPublisher"/>).
/// Business logic lives here per ADR-02; controllers are intentionally thin.
/// </summary>
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

    /// <summary>
    /// Returns a single work item by its unique identifier.
    /// </summary>
    /// <exception cref="NotFoundException">
    /// Thrown when no work item with the given <paramref name="id"/> exists;
    /// caught by <c>GlobalExceptionMiddleware</c> and translated to HTTP 404.
    /// </exception>
    public async Task<WorkItemResponse> GetByIdAsync(Guid id)
    {
        var workItem = await _dbContext.WorkItems.FirstOrDefaultAsync(x => x.Id == id);

        if (workItem is null)
            throw new NotFoundException($"Work item with ID {id} not found.");

        return WorkItemResponse.FromEntity(workItem);
    }

    /// <summary>
    /// Returns a paginated, optionally filtered and sorted slice of work items.
    /// Filters are combined with AND semantics; all filter parameters are optional.
    /// </summary>
    /// <remarks>
    /// Page and page size are clamped to safe bounds (minimum 1, maximum 100 items per page)
    /// to prevent unbounded queries regardless of what the client sends.
    /// The total count is computed before sorting and pagination so the caller can
    /// calculate total pages without a second round-trip.
    /// Default sort is <c>createdAt desc</c> — newest first, consistent with the UI default.
    /// </remarks>
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

    /// <summary>
    /// Persists a new work item and notifies all connected clients and downstream subscribers.
    /// </summary>
    /// <remarks>
    /// Status is always initialised to <see cref="WorkItemStatus.Todo"/> — the API does not
    /// accept an initial status from the caller; progression is an explicit update.
    /// <para>
    /// <see cref="Task.WhenAll"/> fires the SignalR broadcast and the Service Bus event
    /// concurrently after the database commit. The two side effects are independent,
    /// so parallelising them reduces end-to-end latency without introducing ordering risk.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Applies the requested mutations to an existing work item and notifies subscribers.
    /// </summary>
    /// <remarks>
    /// The previous status is captured before the entity is mutated because the event payload
    /// carries both the old and new status — downstream consumers need the delta to decide
    /// whether to act (e.g. trigger a notification only on status transitions to <c>Done</c>).
    /// </remarks>
    /// <exception cref="NotFoundException">
    /// Thrown when no work item with the given <paramref name="id"/> exists.
    /// </exception>
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

    /// <summary>
    /// Removes a work item permanently and notifies all connected clients and downstream subscribers.
    /// </summary>
    /// <exception cref="NotFoundException">
    /// Thrown when no work item with the given <paramref name="id"/> exists.
    /// </exception>
    public async Task DeleteAsync(Guid id)
    {
        var workItem = await _dbContext.WorkItems.FirstOrDefaultAsync(x => x.Id == id);

        if (workItem is null)
            throw new NotFoundException($"Work item with ID {id} not found.");

        // Capture title and id before removal — the entity is detached from the change tracker
        // after SaveChanges, so these values are no longer accessible for logging or the event payload.
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

    /// <summary>
    /// Applies a sort order to the query based on the caller-supplied field and direction.
    /// Falls back to <c>createdAt desc</c> for any unrecognised <paramref name="sortBy"/> value,
    /// guaranteeing a stable, deterministic order regardless of client input.
    /// </summary>
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
