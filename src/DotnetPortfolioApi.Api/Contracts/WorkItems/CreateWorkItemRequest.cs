namespace DotnetPortfolioApi.Api.Contracts.WorkItems;

using DotnetPortfolioApi.Api.Domain;
using System.ComponentModel.DataAnnotations;

public class CreateWorkItemRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 255 characters")]
    public string Title { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Priority is required")]
    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
}
