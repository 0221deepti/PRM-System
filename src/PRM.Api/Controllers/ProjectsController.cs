using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Project;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IMilestoneService _milestoneService;

    public ProjectsController(IProjectService projectService, IMilestoneService milestoneService)
    {
        _projectService = projectService;
        _milestoneService = milestoneService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        var result = await _projectService.CreateProjectAsync(dto, ct);
        return CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? scope, CancellationToken ct)
    {
        if (scope == "mine")
        {
            var managerEmpId = GetCallerEmployeeId();
            var mine = await _projectService.GetManagerProjectsAsync(managerEmpId, ct);
            return Ok(mine);
        }

        if (!User.IsInRole("Admin"))
            return Forbid();

        var all = await _projectService.GetAllProjectsAsync(ct);
        return Ok(all);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var project = await _projectService.GetProjectDetailAsync(id, ct);
        if (project == null) return NotFound();
        return Ok(project);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto, CancellationToken ct)
    {
        await _projectService.UpdateProjectAsync(id, dto, ct);
        return Ok(new { message = "Project updated." });
    }

    [HttpPost("{id:int}/milestones")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddMilestone(int id, [FromBody] AddMilestoneDto dto, CancellationToken ct)
    {
        var result = await _milestoneService.AddMilestoneAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpPut("{projectId:int}/milestones/{milestoneId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateMilestoneStatus(int projectId, int milestoneId, [FromBody] UpdateMilestoneStatusDto dto, CancellationToken ct)
    {
        await _milestoneService.UpdateMilestoneStatusAsync(milestoneId, dto, ct);
        return Ok(new { message = "Milestone updated." });
    }

    [HttpGet("{id:int}/milestones")]
    public async Task<IActionResult> GetMilestones(int id, CancellationToken ct)
    {
        var milestones = await _milestoneService.GetProjectMilestonesAsync(id, ct);
        return Ok(milestones);
    }

    private int GetCallerEmployeeId()
        => int.Parse(User.FindFirstValue("employeeId")
               ?? throw new InvalidOperationException("Employee ID claim missing."));
}
