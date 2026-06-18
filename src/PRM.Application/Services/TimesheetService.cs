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
    private readonly IUserRepository _users;
    private readonly IRepository<ActivityTag> _tags;
    private readonly IRepository<TimesheetEntry> _entries;
    private readonly IRepository<TimesheetEntryTag> _entryTags;

    public TimesheetService(
        ITimesheetRepository timesheets,
        IAllocationRepository allocations,
        ISystemConfigRepository config,
        IUserRepository users,
        IRepository<ActivityTag> tags,
        IRepository<TimesheetEntry> entries,
        IRepository<TimesheetEntryTag> entryTags)
    {
        _timesheets = timesheets;
        _allocations = allocations;
        _config = config;
        _users = users;
        _tags = tags;
        _entries = entries;
        _entryTags = entryTags;
    }

    public async Task SubmitAsync(SubmitTimesheetDto dto, int userId, CancellationToken ct)
    {
        var config = await _config.GetAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (dto.WeekStartDate > today)
            throw new DomainException("Cannot submit a timesheet for a future week.");

        if (await _timesheets.ExistsAsync(userId, dto.ProjectId, dto.WeekStartDate, ct))
            throw new DuplicateTimesheetException("A timesheet for this project and week already exists.");

        // Verify the user is allocated to this project during the week
        var activeAllocations = await _allocations.GetActiveByUserAsync(userId, ct);
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

        var totalThisWeek = await _timesheets.GetTotalHoursForWeekAsync(userId, dto.WeekStartDate, ct);
        if (totalThisWeek + dto.HoursWorked > config.MaxWeeklyHours)
            throw new DomainException(
                $"Total hours this week would exceed {config.MaxWeeklyHours} hrs.");

        var timesheet = new Timesheet
        {
            UserId = userId,
            ProjectId = dto.ProjectId,
            WeekStartDate = dto.WeekStartDate,
            TotalHoursWorked = dto.HoursWorked
        };

        await _timesheets.AddAsync(timesheet, ct);
        await _timesheets.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<TimesheetSummaryDto>> GetMyTimesheetsAsync(int userId, CancellationToken ct)
    {
        var timesheets = await _timesheets.GetByUserAsync(userId, ct);
        return timesheets.Select(t => new TimesheetSummaryDto(
            t.Id, t.ProjectId, t.Project?.Name ?? "",
            t.WeekStartDate, t.TotalHoursWorked,
            string.Join(",", t.Entries.SelectMany(e => e.Tags.Select(tag => tag.ActivityTag.Name))),
            true));
    }

    public async Task<IEnumerable<TeamTimesheetEntryDto>> GetTeamTimesheetsAsync(int managerId, DateOnly weekStart, CancellationToken ct)
    {
        var timesheets = await _timesheets.GetByWeekAsync(weekStart, managerId, ct);
        return timesheets.Select(t => new TeamTimesheetEntryDto(
            t.User?.FullName ?? "",
            t.UserId,
            t.Project?.Name ?? "",
            t.ProjectId,
            t.TotalHoursWorked,
            true,
            t.WeekStartDate));
    }

    public async Task<bool> HasMissedLastWeekAsync(int userId, CancellationToken ct)
    {
        var lastMonday = GetLastMonday();
        var activeAllocations = await _allocations.GetActiveByUserAsync(userId, ct);
        var allocationsLastWeek = activeAllocations
            .Where(a => a.FromDate <= lastMonday && a.ToDate >= lastMonday)
            .ToList();

        if (!allocationsLastWeek.Any()) return false;

        var timesheets = await _timesheets.GetByUserAsync(userId, ct);
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
