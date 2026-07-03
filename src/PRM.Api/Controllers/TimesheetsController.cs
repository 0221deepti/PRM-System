using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Common;
using PRM.Application.DTOs.Notification;
using PRM.Application.DTOs.Timesheet;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

/// <summary>
/// Timesheet management - Submit and view timesheets, manage access.
/// </summary>
[ApiController]
[Route("api/timesheets")]
[Authorize]
[Produces("application/json")]
public class TimesheetsController : ControllerBase
{
    private readonly ITimesheetService _timesheetService;
    private readonly ITimesheetAccessService _accessService;

    public TimesheetsController(ITimesheetService timesheetService, ITimesheetAccessService accessService)
    {
        _timesheetService = timesheetService;
        _accessService = accessService;
    }

    /// <summary>
    /// Submit a timesheet (Employee only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Submit([FromBody] SubmitTimesheetDto dto, CancellationToken ct)
    {
        var employeeId = GetCallerEmployeeId();
        await _timesheetService.SubmitAsync(dto, employeeId, ct);
        return Ok(new ApiResponse(true, "Timesheet submitted successfully."));
    }

    /// <summary>
    /// Get authenticated employee's own timesheets
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var employeeId = GetCallerEmployeeId();
        var timesheets = await _timesheetService.GetMyTimesheetsAsync(employeeId, ct);
        return Ok(new ApiResponse<IEnumerable<TimesheetSummaryDto>>(true, "My timesheets retrieved successfully.", timesheets));
    }

    /// <summary>
    /// Get team timesheets for manager (Manager/Admin only)
    /// </summary>
    [HttpGet("team")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetTeam([FromQuery] DateOnly? weekStart, CancellationToken ct)
    {
        if (!weekStart.HasValue)
            return BadRequest(new ApiResponse(false, "Week Start Date is invalid or missing."));

        var managerId = GetCallerEmployeeId();
        var timesheets = await _timesheetService.GetTeamTimesheetsAsync(managerId, weekStart.Value, ct);
        return Ok(new ApiResponse<IEnumerable<TeamTimesheetEntryDto>>(true, "Team timesheets retrieved successfully.", timesheets));
    }

    /// <summary>
    /// Check if authenticated employee missed last week's timesheet submission
    /// </summary>
    [HttpGet("missed-last-week")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMissedLastWeek(CancellationToken ct)
    {
        var employeeId = GetCallerEmployeeId();
        var result = await _timesheetService.HasMissedLastWeekAsync(employeeId, ct);
        return Ok(new ApiResponse<object>(true, "Missed last week check completed.", new { missedLastWeek = result }));
    }

    /// <summary>
    /// Get timesheet access status for employee
    /// </summary>
    [HttpGet("access-status/{employeeId:int}")]
    public async Task<IActionResult> GetAccessStatus(int employeeId, CancellationToken ct)
    {
        var callerId = GetCallerEmployeeId();
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

        var status = await _accessService.GetCurrentStatusAsync(employeeId, callerId, callerRole, ct);
        if (status == null)
            return NotFound(new ApiResponse(false, "Access status not found for this employee."));

        return Ok(new ApiResponse<TimesheetAccessStatusDto>(true, "Access status retrieved successfully.", status));
    }

    /// <summary>
    /// Restore timesheet access for employee (Manager only)
    /// </summary>
    [HttpPost("access-status/restore/{employeeId:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> RestoreAccess(int employeeId, CancellationToken ct)
    {
        var managerId = GetCallerEmployeeId();
        var result = await _accessService.RestoreAccessAsync(managerId, employeeId, ct);
        return Ok(new ApiResponse<TimesheetAccessStatusDto>(true, "Access restored successfully.", result));
    }

    /// <summary>
    /// Manually trigger timesheet reminder process (Admin only) - For testing and operational purposes
    /// </summary>
    [HttpPost("reminders/process")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProcessReminders(CancellationToken ct)
    {
        await _accessService.ProcessDailyAsync(ct);
        
        var result = new RemindersProcessResultDto(
            Success: true,
            EmployeesChecked: 0,
            Reminders1Sent: 0,
            Reminders2Sent: 0,
            AccountsFrozen: 0,
            AlreadySubmitted: 0,
            Message: "Reminder process executed successfully. Check logs for details.",
            ProcessedAt: DateTime.UtcNow);
        
        return Ok(new ApiResponse<RemindersProcessResultDto>(
            true, 
            "Timesheet reminder process triggered successfully.", 
            result));
    }

    private int GetCallerEmployeeId()
        => int.Parse(User.FindFirstValue("employeeId")
               ?? throw new InvalidOperationException("Employee ID claim missing."));
}
