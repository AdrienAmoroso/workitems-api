namespace DotnetPortfolioApi.Api.Domain;

public class WorkItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Todo;
    
    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum WorkItemStatus
{
    Todo,
    InProgress,
    Done
}

public enum WorkItemPriority
{
    Low,
    Medium,
    High
}
