using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Common;
using PRM.Application.DTOs.Project;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

/// <summary>
/// Project management - Create and manage projects with milestones and health tracking.
/// </summary>
[ApiController]
[Route("api/projects")]
[Authorize]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IMilestoneService _milestoneService;

    public ProjectsController(IProjectService projectService, IMilestoneService milestoneService)
    {
        _projectService = projectService;
        _milestoneService = milestoneService;
    }

    /// <summary>
    /// Create new project (Admin only)
    /// </summary>
    /// <param name="dto">Project details - name, description, dates, manager</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="201">Project created successfully</response>
    /// <response code="400">Invalid project data</response>
    /// <response code="403">Forbidden - Admin only</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        var result = await _projectService.CreateProjectAsync(dto, ct);
        return CreatedAtAction(nameof(GetDetail), new { id = result.Id }, new ApiResponse<ProjectSummaryDto>(true, "Project created successfully.", result));
    }

    /// <summary>
    /// Get projects - Admin gets all, Managers get their projects
    /// </summary>
    /// <param name="scope">Optional filter: 'mine' for manager's projects</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">List of projects</response>
    /// <response code="403">Forbidden for non-admin/manager</response>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? scope, CancellationToken ct)
    {
        if (scope == "mine")
        {
            var managerEmpId = GetCallerEmployeeId();
            var mine = await _projectService.GetManagerProjectsAsync(managerEmpId, ct);
            return Ok(new ApiResponse<IEnumerable<ProjectSummaryDto>>(true, "Manager projects retrieved successfully.", mine));
        }

        if (!User.IsInRole("Admin"))
            return StatusCode(403, new ApiResponse(false, "Forbidden - Admin role required."));

        var all = await _projectService.GetAllProjectsAsync(ct);
        return Ok(new ApiResponse<IEnumerable<ProjectSummaryDto>>(true, "All projects retrieved successfully.", all));
    }

    /// <summary>
    /// Get project details including allocations and milestones
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Project details</response>
    /// <response code="404">Project not found</response>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var callerId = GetCallerEmployeeId();
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var project = await _projectService.GetProjectDetailAsync(id, callerId, callerRole, ct);
        if (project == null) return NotFound(new ApiResponse(false, "Project not found."));
        return Ok(new ApiResponse<ProjectDetailDto>(true, "Project details retrieved successfully.", project));
    }

    /// <summary>
    /// Update project information (Admin only)
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="dto">Updated project data</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Project updated</response>
    /// <response code="403">Forbidden - Admin only</response>
    /// <response code="404">Project not found</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto, CancellationToken ct)
    {
        await _projectService.UpdateProjectAsync(id, dto, ct);
        return Ok(new ApiResponse(true, "Project updated successfully."));
    }

    /// <summary>
    /// Add milestone to project (Admin only)
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="dto">Milestone - name, description, target date, story points</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Milestone created</response>
    /// <response code="403">Forbidden - Admin only</response>
    /// <response code="404">Project not found</response>
    [HttpPost("{id:int}/milestones")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddMilestone(int id, [FromBody] AddMilestoneDto dto, CancellationToken ct)
    {
        var callerId = GetCallerEmployeeId();
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var result = await _milestoneService.AddMilestoneAsync(id, dto, callerId, callerRole, ct);
        return Ok(new ApiResponse<MilestoneSummaryDto>(true, "Milestone added successfully.", result));
    }

    /// <summary>
    /// Update milestone status (Admin/Manager)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="dto">New status (Pending, InProgress, Completed, OnHold)</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Milestone status updated</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Milestone not found</response>
    [HttpPut("{projectId:int}/milestones/{milestoneId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateMilestoneStatus(int projectId, int milestoneId, [FromBody] UpdateMilestoneStatusDto dto, CancellationToken ct)
    {
        var callerId = GetCallerEmployeeId();
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
        await _milestoneService.UpdateMilestoneStatusAsync(milestoneId, dto, callerId, callerRole, ct);
        return Ok(new ApiResponse(true, "Milestone status updated successfully."));
    }

    /// <summary>
    /// Get all milestones for a project
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">List of milestones</response>
    /// <response code="404">Project not found</response>
    [HttpGet("{id:int}/milestones")]
    public async Task<IActionResult> GetMilestones(int id, CancellationToken ct)
    {
        var callerId = GetCallerEmployeeId();
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var milestones = await _milestoneService.GetProjectMilestonesAsync(id, callerId, callerRole, ct);
        return Ok(new ApiResponse<IEnumerable<MilestoneSummaryDto>>(true, "Milestones retrieved successfully.", milestones));
    }

    private int GetCallerEmployeeId()
        => int.Parse(User.FindFirstValue("employeeId")
               ?? throw new InvalidOperationException("Employee ID claim missing."));
}
