namespace WorkItems.Api.Endpoints;

using WorkItems.Api.Contracts.WorkItems;
using WorkItems.Api.Domain;
using WorkItems.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/work-items")]
public class WorkItemsController : ControllerBase
{
    private readonly IWorkItemService _workItemService;

    public WorkItemsController(IWorkItemService workItemService)
    {
        _workItemService = workItemService;
    }

    /// <summary>
    /// Get all work items with filtering, pagination, and sorting
    /// </summary>
    /// <remarks>
    /// Query Parameters:
    /// - page: Page number (default: 1)
    /// - pageSize: Items per page (default: 10, max: 100)
    /// - status: Filter by status (Todo, InProgress, Done)
    /// - priority: Filter by priority (Low, Medium, High)
    /// - sortBy: Sort field (createdAt, updatedAt, title, status, priority) (default: createdAt)
    /// - sortDir: Sort direction (asc, desc) (default: desc)
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<WorkItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<WorkItemResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] WorkItemStatus? status = null,
        [FromQuery] WorkItemPriority? priority = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDir = "desc")
    {
        var result = await _workItemService.GetAllAsync(page, pageSize, status, priority, sortBy, sortDir);
        return Ok(result);
    }

    /// <summary>
    /// Get a work item by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkItemResponse>> GetById(Guid id)
    {
        try
        {
            var result = await _workItemService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new work item
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanManageWorkItems")]
    [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkItemResponse>> Create([FromBody] CreateWorkItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _workItemService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing work item
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageWorkItems")]
    [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkItemResponse>> Update(Guid id, [FromBody] UpdateWorkItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _workItemService.UpdateAsync(id, request);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a work item
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanDeleteWorkItems")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _workItemService.DeleteAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
