namespace DotnetPortfolioApi.Api.Services;

using DotnetPortfolioApi.Api.Contracts.WorkItems;
using DotnetPortfolioApi.Api.Data;
using DotnetPortfolioApi.Api.Domain;
using Microsoft.EntityFrameworkCore;

public class WorkItemService : IWorkItemService
{
    private readonly AppDbContext _dbContext;

    public WorkItemService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
        // Validate pagination
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _dbContext.WorkItems.AsQueryable();

        // Apply filters
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        
        if (priority.HasValue)
            query = query.Where(x => x.Priority == priority.Value);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, sortBy, sortDir);

        // Apply pagination
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

        return WorkItemResponse.FromEntity(workItem);
    }

    public async Task<WorkItemResponse> UpdateAsync(Guid id, UpdateWorkItemRequest request)
    {
        var workItem = await _dbContext.WorkItems.FirstOrDefaultAsync(x => x.Id == id);
        
        if (workItem is null)
            throw new NotFoundException($"Work item with ID {id} not found.");

        workItem.Title = request.Title;
        workItem.Description = request.Description;
        workItem.Status = request.Status;
        workItem.Priority = request.Priority;
        workItem.UpdatedAt = DateTime.UtcNow;

        _dbContext.WorkItems.Update(workItem);
        await _dbContext.SaveChangesAsync();

        return WorkItemResponse.FromEntity(workItem);
    }

    public async Task DeleteAsync(Guid id)
    {
        var workItem = await _dbContext.WorkItems.FirstOrDefaultAsync(x => x.Id == id);
        
        if (workItem is null)
            throw new NotFoundException($"Work item with ID {id} not found.");

        _dbContext.WorkItems.Remove(workItem);
        await _dbContext.SaveChangesAsync();
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
