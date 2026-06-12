using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

/// <summary>
/// Employee management - View and manage team members, skills, and allocations.
/// </summary>
[ApiController]
[Route("api/employees")]
[Authorize]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ISkillService _skillService;

    public EmployeesController(IEmployeeService employeeService, ISkillService skillService)
    {
        _employeeService = employeeService;
        _skillService = skillService;
    }

    /// <summary>
    /// Get employees - Admin gets all, Managers get their team
    /// </summary>
    /// <param name="scope">Optional filter: 'my-team' for manager's team</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">List of employees</response>
    /// <response code="403">Forbidden for non-admin/manager</response>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? scope, CancellationToken ct)
    {
        if (scope == "my-team")
        {
            var managerEmpId = GetCallerEmployeeId();
            var team = await _employeeService.GetTeamEmployeesAsync(managerEmpId, ct);
            return Ok(team);
        }

        // Admin only
        if (!User.IsInRole("Admin"))
            return Forbid();

        var all = await _employeeService.GetAllEmployeesAsync(ct);
        return Ok(all);
    }

    /// <summary>
    /// Get detailed employee information
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Employee detail with skills and allocations</response>
    /// <response code="404">Employee not found</response>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var emp = await _employeeService.GetEmployeeDetailAsync(id, ct);
        if (emp == null) return NotFound();
        return Ok(emp);
    }

    /// <summary>
    /// Get authenticated employee's own profile
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Current user's employee profile</response>
    /// <response code="404">Employee profile not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("me")]
    [Authorize(Roles = "Employee,Manager")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var emp = await _employeeService.GetEmployeeByUserIdAsync(userId, ct);
        if (emp == null) return NotFound();
        return Ok(emp);
    }

    /// <summary>
    /// Update employee information (Admin only)
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="dto">Updated employee data (department, etc.)</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Employee updated</response>
    /// <response code="403">Forbidden - Admin required</response>
    /// <response code="404">Employee not found</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto, CancellationToken ct)
    {
        await _employeeService.UpdateEmployeeAsync(id, dto, ct);
        return Ok(new { message = "Employee updated." });
    }

    /// <summary>
    /// Deactivate employee (Admin only)
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Employee deactivated</response>
    /// <response code="403">Forbidden - Admin required</response>
    /// <response code="404">Employee not found</response>
    [HttpPut("{id:int}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _employeeService.DeactivateEmployeeAsync(id, ct);
        return Ok(new { message = "Employee deactivated." });
    }

    /// <summary>
    /// Assign manager to employee (Admin only)
    /// </summary>
    /// <param name="id">Employee ID (not used, kept for routing)</param>
    /// <param name="dto">Employee and Manager user IDs</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Manager assigned</response>
    /// <response code="403">Forbidden - Admin required</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id:int}/assign-manager")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignManager(int id, [FromBody] AssignManagerDto dto, CancellationToken ct)
    {
        await _employeeService.AssignManagerAsync(dto.EmployeeUserId, dto.ManagerUserId, ct);
        return Ok(new { message = "Manager assigned." });
    }

    /// <summary>
    /// Add skill to employee (Admin only)
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="dto">Skill name, category, and proficiency level</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Skill added</response>
    /// <response code="400">Skill already assigned or invalid</response>
    /// <response code="403">Forbidden - Admin required</response>
    /// <response code="404">Employee not found</response>
    [HttpPost("{id:int}/skills")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddSkill(int id, [FromBody] AddSkillDto dto, CancellationToken ct)
    {
        await _skillService.AddSkillAsync(id, dto, ct);
        return Ok(new { message = "Skill added." });
    }

    /// <summary>
    /// Update employee skill proficiency (Admin only)
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="skillId">Skill ID</param>
    /// <param name="dto">New proficiency level</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Skill updated</response>
    /// <response code="403">Forbidden - Admin required</response>
    /// <response code="404">Employee or skill not found</response>
    [HttpPut("{id:int}/skills/{skillId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSkill(int id, int skillId, [FromBody] UpdateSkillDto dto, CancellationToken ct)
    {
        await _skillService.UpdateSkillProficiencyAsync(id, skillId, dto, ct);
        return Ok(new { message = "Skill updated." });
    }

    /// <summary>
    /// Remove skill from employee (Admin only)
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="skillId">Skill ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Skill removed</response>
    /// <response code="403">Forbidden - Admin required</response>
    /// <response code="404">Employee or skill not found</response>
    [HttpDelete("{id:int}/skills/{skillId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveSkill(int id, int skillId, CancellationToken ct)
    {
        await _skillService.RemoveSkillAsync(id, skillId, ct);
        return Ok(new { message = "Skill removed." });
    }

    private int GetCallerEmployeeId()
        => int.Parse(User.FindFirstValue("employeeId")
               ?? throw new InvalidOperationException("Employee ID claim missing."));
}
