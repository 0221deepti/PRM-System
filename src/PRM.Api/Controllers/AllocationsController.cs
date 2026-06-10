using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Allocation;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/allocations")]
[Authorize]
public class AllocationsController : ControllerBase
{
    private readonly IAllocationService _service;

    public AllocationsController(IAllocationService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var allocations = await _service.GetAllAllocationsAsync(ct);
        return Ok(allocations);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var empId = GetCallerEmployeeId();
        var allocations = await _service.GetMyAllocationsAsync(empId, ct);
        return Ok(allocations);
    }

    [HttpGet("project/{projectId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetByProject(int projectId, CancellationToken ct)
    {
        var allocations = await _service.GetActiveAllocationsByProjectAsync(projectId, ct);
        return Ok(allocations);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateAllocationDto dto, CancellationToken ct)
    {
        var managerEmployeeId = GetCallerEmployeeId();
        var result = await _service.AllocateAsync(dto, managerEmployeeId, ct);
        return Ok(result);
    }

    [HttpPut("{id:int}/end")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> End(int id, CancellationToken ct)
    {
        var managerEmployeeId = GetCallerEmployeeId();
        await _service.EndAllocationAsync(id, managerEmployeeId, ct);
        return Ok(new { message = "Allocation ended." });
    }

    private int GetCallerEmployeeId()
        => int.Parse(User.FindFirstValue("employeeId")
               ?? throw new InvalidOperationException("Employee ID claim missing."));
}
