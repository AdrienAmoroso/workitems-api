namespace DotnetPortfolioApi.Api.Domain;

public class WorkItem
{
    public int Id { get; set; }
    
    public string Title { get; set; } = null!;
    
    public string Description { get; set; } = null!;
    
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Open;
    
    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum WorkItemStatus
{
    Open,
    InProgress,
    Completed,
    Closed
}

public enum WorkItemPriority
{
    Low,
    Medium,
    High,
    Critical
}
