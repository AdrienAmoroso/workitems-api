namespace DotnetPortfolioApi.Api.Services;

using DotnetPortfolioApi.Api.Contracts.WorkItems;
using DotnetPortfolioApi.Api.Domain;

public interface IWorkItemService
{
    Task<WorkItemResponse> GetByIdAsync(Guid id);
    
    Task<PaginatedResult<WorkItemResponse>> GetAllAsync(
        int page = 1,
        int pageSize = 10,
        WorkItemStatus? status = null,
        WorkItemPriority? priority = null,
        string sortBy = "createdAt",
        string sortDir = "desc");
    
    Task<WorkItemResponse> CreateAsync(CreateWorkItemRequest request);
    
    Task<WorkItemResponse> UpdateAsync(Guid id, UpdateWorkItemRequest request);
    
    Task DeleteAsync(Guid id);
}

public class PaginatedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<T> Items { get; set; } = [];

    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
