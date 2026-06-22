using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Allocation;
using PRM.Application.DTOs.Common;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

/// <summary>
/// Resource allocation management - Allocate employees to projects with utilization tracking.
/// </summary>
[ApiController]
[Route("api/allocations")]
[Authorize]
[Produces("application/json")]
public class AllocationsController : ControllerBase
{
    private readonly IAllocationService _service;

    public AllocationsController(IAllocationService service) => _service = service;

    /// <summary>
    /// Get all allocations (Admin only)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">List of all active allocations</response>
    /// <response code="403">Forbidden - Admin only</response>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var allocations = await _service.GetAllAllocationsAsync(ct);
        return Ok(new ApiResponse<IEnumerable<AllocationSummaryDto>>(true, "All allocations retrieved successfully.", allocations));
    }

    /// <summary>
    /// Get employee's own allocations
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Employee's active allocations</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("mine")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var empId = GetCallerEmployeeId();
        var allocations = await _service.GetMyAllocationsAsync(empId, ct);
        return Ok(new ApiResponse<IEnumerable<AllocationSummaryDto>>(true, "My allocations retrieved successfully.", allocations));
    }

    /// <summary>
    /// Get allocations for a specific project (Manager/Admin only)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Project's allocations</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Project not found</response>
    [HttpGet("project/{projectId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetByProject(int projectId, CancellationToken ct)
    {
        var callerId = GetCallerEmployeeId();
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var allocations = await _service.GetActiveAllocationsByProjectAsync(projectId, callerId, callerRole, ct);
        return Ok(new ApiResponse<IEnumerable<AllocationSummaryDto>>(true, "Project allocations retrieved successfully.", allocations));
    }

    /// <summary>
    /// Create new allocation (Manager only) - Validates over-allocation
    /// </summary>
    /// <param name="dto">Employee ID, Project ID, utilization percentage, dates</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Allocation created successfully</response>
    /// <response code="400">Over-allocation or validation error</response>
    /// <response code="403">Forbidden - Manager only</response>
    /// <response code="404">Employee or project not found</response>
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateAllocationDto dto, CancellationToken ct)
    {
        var managerEmployeeId = GetCallerEmployeeId();
        var (result, warning) = await _service.AllocateAsync(dto, managerEmployeeId, ct);
        var msg = warning ?? "Allocation created successfully.";
        return Ok(new ApiResponse<AllocationSummaryDto>(true, msg, result));
    }

    /// <summary>
    /// End an allocation (Manager only)
    /// </summary>
    /// <param name="id">Allocation ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Allocation ended</response>
    /// <response code="403">Forbidden - Manager only</response>
    /// <response code="404">Allocation not found</response>
    [HttpPut("{id:int}/end")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> End(int id, CancellationToken ct)
    {
        var managerEmployeeId = GetCallerEmployeeId();
        await _service.EndAllocationAsync(id, managerEmployeeId, ct);
        return Ok(new ApiResponse(true, "Allocation ended successfully."));
    }

    private int GetCallerEmployeeId()
        => int.Parse(User.FindFirstValue("employeeId")
               ?? throw new InvalidOperationException("Employee ID claim missing."));
}
