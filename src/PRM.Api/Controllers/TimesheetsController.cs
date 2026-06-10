using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Timesheet;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/timesheets")]
[Authorize]
public class TimesheetsController : ControllerBase
{
    private readonly ITimesheetService _timesheetService;
    private readonly IAllocationService _allocationService;

    public TimesheetsController(ITimesheetService timesheetService, IAllocationService allocationService)
    {
        _timesheetService = timesheetService;
        _allocationService = allocationService;
    }

    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Submit([FromBody] SubmitTimesheetDto dto, CancellationToken ct)
    {
        var empId = GetCallerEmployeeId();
        await _timesheetService.SubmitAsync(dto, empId, ct);
        return Ok(new { message = "Timesheet submitted successfully." });
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var empId = GetCallerEmployeeId();
        var timesheets = await _timesheetService.GetMyTimesheetsAsync(empId, ct);
        return Ok(timesheets);
    }

    [HttpGet("team")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetTeam([FromQuery] string? week, CancellationToken ct)
    {
        var managerId = GetCallerEmployeeId();
        DateOnly weekStart;

        if (!string.IsNullOrEmpty(week))
        {
            if (!DateOnly.TryParseExact(week, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out weekStart))
                return BadRequest(new { error = "Invalid week format. Use DD-MM-YYYY." });
        }
        else
        {
            // Current week's Monday
            var today = DateTime.UtcNow;
            var daysBack = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            weekStart = DateOnly.FromDateTime(today.AddDays(-daysBack));
        }

        var timesheets = await _timesheetService.GetTeamTimesheetsAsync(managerId, weekStart, ct);
        return Ok(timesheets);
    }

    [HttpGet("missed-check")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> CheckMissed(CancellationToken ct)
    {
        var empId = GetCallerEmployeeId();
        var hasMissed = await _timesheetService.HasMissedLastWeekAsync(empId, ct);
        return Ok(new { hasMissed });
    }

    [HttpGet("week-allocations")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetWeekAllocations([FromQuery] string week, CancellationToken ct)
    {
        var empId = GetCallerEmployeeId();
        if (!DateOnly.TryParseExact(week, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var weekStart))
            return BadRequest(new { error = "Invalid week format. Use DD-MM-YYYY." });

        var allocations = await _allocationService.GetActiveAllocationsForWeekAsync(empId, weekStart, ct);
        return Ok(allocations);
    }

    private int GetCallerEmployeeId()
        => int.Parse(User.FindFirstValue("employeeId")
               ?? throw new InvalidOperationException("Employee ID claim missing."));
}
