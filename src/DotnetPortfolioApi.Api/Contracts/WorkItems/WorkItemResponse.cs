namespace DotnetPortfolioApi.Api.Contracts.WorkItems;

using DotnetPortfolioApi.Api.Domain;

public class WorkItemResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public WorkItemStatus Status { get; set; }

    public WorkItemPriority Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static WorkItemResponse FromEntity(WorkItem workItem)
    {
        return new WorkItemResponse
        {
            Id = workItem.Id,
            Title = workItem.Title,
            Description = workItem.Description,
            Status = workItem.Status,
            Priority = workItem.Priority,
            CreatedAt = workItem.CreatedAt,
            UpdatedAt = workItem.UpdatedAt
        };
    }
}
