using PRM.Application.DTOs.Timesheet;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Application.Services;

public class TimesheetService : ITimesheetService
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IAllocationRepository _allocations;
    private readonly ISystemConfigRepository _config;
    private readonly IEmployeeRepository _employees;

    public TimesheetService(
        ITimesheetRepository timesheets,
        IAllocationRepository allocations,
        ISystemConfigRepository config,
        IEmployeeRepository employees)
    {
        _timesheets = timesheets;
        _allocations = allocations;
        _config = config;
        _employees = employees;
    }

    public async Task SubmitAsync(SubmitTimesheetDto dto, int employeeId, CancellationToken ct)
    {
        var config = await _config.GetAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (dto.WeekStartDate > today)
            throw new DomainException("Cannot submit a timesheet for a future week.");

        if (await _timesheets.ExistsAsync(employeeId, dto.ProjectId, dto.WeekStartDate, ct))
            throw new DuplicateTimesheetException("A timesheet for this project and week already exists.");

        // Verify the employee is allocated to this project during the week
        var activeAllocations = await _allocations.GetActiveByEmployeeAsync(employeeId, ct);
        var allocation = activeAllocations.FirstOrDefault(a =>
            a.ProjectId == dto.ProjectId &&
            a.FromDate <= dto.WeekStartDate &&
            a.ToDate >= dto.WeekStartDate);

        if (allocation == null)
            throw new DomainException("You are not allocated to this project during the specified week.");

        var maxHoursForProject = allocation.UtilisationPercent / 100m * config.MaxWeeklyHours;
        if (dto.HoursWorked > maxHoursForProject)
            throw new DomainException(
                $"Hours logged ({dto.HoursWorked}) exceed the allowed maximum for this allocation ({maxHoursForProject} hrs).");

        var totalThisWeek = await _timesheets.GetTotalHoursForWeekAsync(employeeId, dto.WeekStartDate, ct);
        if (totalThisWeek + dto.HoursWorked > config.MaxWeeklyHours)
            throw new DomainException(
                $"Total hours this week would exceed {config.MaxWeeklyHours} hrs.");

        var timesheet = new Timesheet
        {
            EmployeeId = employeeId,
            ProjectId = dto.ProjectId,
            WeekStartDate = dto.WeekStartDate,
            HoursWorked = dto.HoursWorked,
            ActivityTags = string.Join(",", dto.ActivityTags)
        };

        await _timesheets.AddAsync(timesheet, ct);
        await _timesheets.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<TimesheetSummaryDto>> GetMyTimesheetsAsync(int employeeId, CancellationToken ct)
    {
        var timesheets = await _timesheets.GetByEmployeeAsync(employeeId, ct);
        return timesheets.Select(t => new TimesheetSummaryDto(
            t.Id, t.ProjectId, t.Project?.Name ?? "",
            t.WeekStartDate, t.HoursWorked, t.ActivityTags, true));
    }

    public async Task<IEnumerable<TeamTimesheetEntryDto>> GetTeamTimesheetsAsync(int managerId, DateOnly weekStart, CancellationToken ct)
    {
        var timesheets = await _timesheets.GetByWeekAsync(weekStart, managerId, ct);
        return timesheets.Select(t => new TeamTimesheetEntryDto(
            t.Employee?.User?.FullName ?? "",
            t.EmployeeId,
            t.Project?.Name ?? "",
            t.ProjectId,
            t.HoursWorked,
            true,
            t.WeekStartDate));
    }

    public async Task<bool> HasMissedLastWeekAsync(int employeeId, CancellationToken ct)
    {
        var lastMonday = GetLastMonday();
        var activeAllocations = await _allocations.GetActiveByEmployeeAsync(employeeId, ct);
        var allocationsLastWeek = activeAllocations
            .Where(a => a.FromDate <= lastMonday && a.ToDate >= lastMonday)
            .ToList();

        if (!allocationsLastWeek.Any()) return false;

        var timesheets = await _timesheets.GetByEmployeeAsync(employeeId, ct);
        var submittedProjectIds = timesheets
            .Where(t => t.WeekStartDate == lastMonday)
            .Select(t => t.ProjectId)
            .ToHashSet();

        return allocationsLastWeek.Any(a => !submittedProjectIds.Contains(a.ProjectId));
    }

    private static DateOnly GetLastMonday()
    {
        var today = DateTime.UtcNow;
        var daysBack = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        if (daysBack == 0) daysBack = 7; // If today is Monday, go back to last Monday
        return DateOnly.FromDateTime(today.AddDays(-daysBack));
    }
}
