using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Timesheet;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

/// <summary>
/// Timesheet management - Employees submit timesheets, managers review team timesheets.
/// </summary>
[ApiController]
[Route("api/timesheets")]
[Authorize]
[Produces("application/json")]
public class TimesheetsController : ControllerBase
{
    private readonly ITimesheetService _timesheetService;
    private readonly IAllocationService _allocationService;

    public TimesheetsController(ITimesheetService timesheetService, IAllocationService allocationService)
    {
        _timesheetService = timesheetService;
        _allocationService = allocationService;
    }

    /// <summary>
    /// Submit weekly timesheet with activity entries and tags (Employee only)
    /// </summary>
    /// <param name="dto">Timesheet data including entries with activity tags</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Timesheet submitted successfully</response>
    /// <response code="400">Validation error - exceeds max hours or invalid entries</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Submit([FromBody] SubmitTimesheetDto dto, CancellationToken ct)
    {
        var empId = GetCallerEmployeeId();
        await _timesheetService.SubmitAsync(dto, empId, ct);
        return Ok(new { message = "Timesheet submitted successfully." });
    }

    /// <summary>
    /// Get employee's own timesheet history (Employee only)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">List of employee's submitted timesheets</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("mine")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var empId = GetCallerEmployeeId();
        var timesheets = await _timesheetService.GetMyTimesheetsAsync(empId, ct);
        return Ok(timesheets);
    }

    /// <summary>
    /// Get team's timesheets for a specific week (Manager only)
    /// </summary>
    /// <param name="week">Optional week start date in DD-MM-YYYY format. Defaults to current week.</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Team's timesheets for the week</response>
    /// <response code="400">Invalid date format</response>
    /// <response code="403">Forbidden - Manager only</response>
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

    /// <summary>
    /// Check if employee missed last week's timesheet submission (Employee only)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Missed check result</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("missed-check")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> CheckMissed(CancellationToken ct)
    {
        var empId = GetCallerEmployeeId();
        var hasMissed = await _timesheetService.HasMissedLastWeekAsync(empId, ct);
        return Ok(new { hasMissed });
    }

    /// <summary>
    /// Get employee's active allocations for a specific week (Employee only)
    /// </summary>
    /// <param name="week">Week start date in DD-MM-YYYY format</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Active allocations for the week</response>
    /// <response code="400">Invalid date format</response>
    /// <response code="401">Unauthorized</response>
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
