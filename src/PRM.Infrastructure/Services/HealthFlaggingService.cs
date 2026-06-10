using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Enums;

namespace PRM.Infrastructure.Services;

public class HealthFlaggingService : IHealthFlaggingService
{
    private readonly IProjectRepository _projects;
    private readonly IEmployeeRepository _employees;
    private readonly IAllocationRepository _allocations;
    private readonly ITimesheetRepository _timesheets;
    private readonly ISystemConfigRepository _config;

    public HealthFlaggingService(
        IProjectRepository projects,
        IEmployeeRepository employees,
        IAllocationRepository allocations,
        ITimesheetRepository timesheets,
        ISystemConfigRepository config)
    {
        _projects = projects;
        _employees = employees;
        _allocations = allocations;
        _timesheets = timesheets;
        _config = config;
    }

    public async Task ComputeEmployeeStatusesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employees = await _employees.GetAllWithDetailsAsync(ct);

        foreach (var employee in employees)
        {
            var activeAllocations = await _allocations.GetActiveByEmployeeAsync(employee.Id, ct);
            var isAllocated = activeAllocations.Any(a => a.FromDate <= today && a.ToDate >= today);
            var newStatus = isAllocated ? EmployeeStatus.Allocated : EmployeeStatus.Bench;

            if (employee.Status != newStatus)
            {
                employee.Status = newStatus;
                _employees.Update(employee);
            }
        }

        await _employees.SaveChangesAsync(ct);
    }

    public async Task FlagProjectHealthAsync(CancellationToken ct)
    {
        var config = await _config.GetAsync(ct);
        var projects = await _projects.GetAllWithDetailsAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastWeek = today.AddDays(-7);

        foreach (var project in projects)
        {
            if (project.Status == ProjectStatus.Completed) continue;

            var health = await ComputeProjectHealthAsync(project, today, lastWeek, config.MaxWeeklyHours, ct);

            if (project.HealthStatus != health)
            {
                project.HealthStatus = health;
                _projects.Update(project);
            }
        }

        await _projects.SaveChangesAsync(ct);
    }

    private async Task<ProjectHealthStatus> ComputeProjectHealthAsync(
        Domain.Entities.Project project, DateOnly today, DateOnly lastWeek,
        int maxWeeklyHours, CancellationToken ct)
    {
        bool isAtRisk = false;
        bool needsAttention = false;

        // Check overdue milestones
        var overdueMilestones = project.Milestones
            .Where(m => m.Status == MilestoneStatus.InProgress && m.DueDate < today)
            .ToList();

        if (overdueMilestones.Any()) isAtRisk = true;

        // Check milestones due within 7 days and not started
        var upcomingNotStarted = project.Milestones
            .Where(m => m.Status == MilestoneStatus.NotStarted && m.DueDate <= today.AddDays(7))
            .ToList();

        if (upcomingNotStarted.Any()) needsAttention = true;

        // Check effort logged last week
        var activeAllocations = project.Allocations
            .Where(a => a.IsActive && a.FromDate <= lastWeek && a.ToDate >= lastWeek)
            .ToList();

        if (activeAllocations.Any())
        {
            decimal totalExpected = activeAllocations.Sum(a => a.UtilisationPercent / 100m * maxWeeklyHours);
            decimal totalActual = 0;

            foreach (var alloc in activeAllocations)
            {
                var empTimesheets = await _timesheets.GetByProjectAndWeekAsync(project.Id, lastWeek, ct);
                var empActual = empTimesheets
                    .Where(t => t.EmployeeId == alloc.EmployeeId)
                    .Sum(t => t.HoursWorked);

                // If any employee logged < 50% expected, mark AT RISK
                var empExpected = alloc.UtilisationPercent / 100m * maxWeeklyHours;
                if (empExpected > 0 && empActual < empExpected * 0.5m) isAtRisk = true;

                totalActual += empActual;
            }

            // If total < 70% of expected, mark ATTENTION
            if (totalExpected > 0 && totalActual < totalExpected * 0.7m) needsAttention = true;
        }

        if (isAtRisk) return ProjectHealthStatus.AtRisk;
        if (needsAttention) return ProjectHealthStatus.Attention;
        return ProjectHealthStatus.OnTrack;
    }
}
