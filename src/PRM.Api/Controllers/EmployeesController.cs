using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ISkillService _skillService;

    public EmployeesController(IEmployeeService employeeService, ISkillService skillService)
    {
        _employeeService = employeeService;
        _skillService = skillService;
    }

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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var emp = await _employeeService.GetEmployeeDetailAsync(id, ct);
        if (emp == null) return NotFound();
        return Ok(emp);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Employee,Manager")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var emp = await _employeeService.GetEmployeeByUserIdAsync(userId, ct);
        if (emp == null) return NotFound();
        return Ok(emp);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto, CancellationToken ct)
    {
        await _employeeService.UpdateEmployeeAsync(id, dto, ct);
        return Ok(new { message = "Employee updated." });
    }

    [HttpPut("{id:int}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _employeeService.DeactivateEmployeeAsync(id, ct);
        return Ok(new { message = "Employee deactivated." });
    }

    [HttpPut("{id:int}/assign-manager")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignManager(int id, [FromBody] AssignManagerDto dto, CancellationToken ct)
    {
        await _employeeService.AssignManagerAsync(dto.EmployeeUserId, dto.ManagerUserId, ct);
        return Ok(new { message = "Manager assigned." });
    }

    [HttpPost("{id:int}/skills")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddSkill(int id, [FromBody] AddSkillDto dto, CancellationToken ct)
    {
        await _skillService.AddSkillAsync(id, dto, ct);
        return Ok(new { message = "Skill added." });
    }

    [HttpPut("{id:int}/skills/{skillId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSkill(int id, int skillId, [FromBody] UpdateSkillDto dto, CancellationToken ct)
    {
        await _skillService.UpdateSkillProficiencyAsync(id, skillId, dto, ct);
        return Ok(new { message = "Skill updated." });
    }

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
