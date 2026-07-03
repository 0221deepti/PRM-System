using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Infrastructure.Services;

public class HealthFlaggingService : IHealthFlaggingService
{
    private readonly IProjectRepository _projects;
    private readonly IEmployeeRepository _employees;
    private readonly IAllocationRepository _allocations;
    private readonly ITimesheetRepository _timesheets;
    private readonly ISystemConfigRepository _config;
    private readonly IProjectRiskNotificationService _projectRiskNotifications;
    private readonly IRepository<AuditLog> _auditLogs;
    private readonly IEmailService _emailService;

    public HealthFlaggingService(
        IProjectRepository projects,
        IEmployeeRepository employees,
        IAllocationRepository allocations,
        ITimesheetRepository timesheets,
        ISystemConfigRepository config,
        IProjectRiskNotificationService projectRiskNotifications,
        IRepository<AuditLog> auditLogs,
        IEmailService emailService)
    {
        _projects = projects;
        _employees = employees;
        _allocations = allocations;
        _timesheets = timesheets;
        _config = config;
        _projectRiskNotifications = projectRiskNotifications;
        _auditLogs = auditLogs;
        _emailService = emailService;
    }

    public async Task ComputeEmployeeStatusesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employees = await _employees.GetAllWithDetailsAsync(ct);

        foreach (var employee in employees)
        {
            var activeAllocations = await _allocations.GetActiveByUserAsync(employee.Id, ct);
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
        var employees = (await _employees.GetAllWithDetailsAsync(ct)).ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastWeek = GetPreviousWeekStart(today);
        var notifications = new List<Application.DTOs.Notification.ProjectRiskNotificationContextDto>();

        foreach (var project in projects)
        {
            if (project.Status == ProjectStatus.Completed) continue;

            var currentHealth = project.HealthStatus;
            var evaluation = await ComputeProjectHealthAsync(project, today, lastWeek, config.MaxWeeklyHours, ct);

            if (project.HealthStatus != evaluation.Status)
            {
                project.HealthStatus = evaluation.Status;
                _projects.Update(project);

                if (currentHealth != ProjectHealthStatus.AtRisk && evaluation.Status == ProjectHealthStatus.AtRisk)
                {
                    notifications.Add(BuildRiskNotificationContext(project, employees, today, evaluation));
                }
            }

            // Check and notify for upcoming milestones
            await CheckAndNotifyUpcomingMilestonesAsync(project, today, ct);
        }

        await _projects.SaveChangesAsync(ct);

        foreach (var notification in notifications)
        {
            await _projectRiskNotifications.NotifyProjectMarkedAtRiskAsync(notification, ct);
        }
    }

    private async Task CheckAndNotifyUpcomingMilestonesAsync(Domain.Entities.Project project, DateOnly today, CancellationToken ct)
    {
        // Milestones due within 3 days and not done yet
        var upcomingMilestones = project.Milestones
            .Where(m => m.Status != MilestoneStatus.Done && m.DueDate <= today.AddDays(3) && m.DueDate >= today)
            .ToList();

        if (!upcomingMilestones.Any()) return;

        var auditLogs = await _auditLogs.GetAllAsync(ct);

        foreach (var milestone in upcomingMilestones)
        {
            var alreadySent = auditLogs.Any(l => 
                l.EventType == AuditEventType.MilestoneUpcomingNotificationSent && 
                l.ProjectId == project.Id && 
                l.Details.Contains($"Milestone: {milestone.Title}"));

            if (alreadySent) continue;

            // Send notification to project manager
            if (project.Manager != null && !string.IsNullOrWhiteSpace(project.Manager.Email))
            {
                var placeholders = new Dictionary<string, string>
                {
                    ["ProjectManagerName"] = project.Manager.FullName,
                    ["MilestoneTitle"] = milestone.Title,
                    ["ProjectName"] = project.Name,
                    ["DueDate"] = milestone.DueDate.ToString("yyyy-MM-dd"),
                    ["StoryPoints"] = milestone.StoryPoints.ToString(),
                    ["MilestoneStatus"] = milestone.Status.ToString()
                };

                var emailResult = await _emailService.SendTemplateEmailAsync(
                    "Milestone Upcoming Notification",
                    project.Manager.Email,
                    placeholders,
                    ct);

                if (emailResult.IsSuccess)
                {
                    await _auditLogs.AddAsync(new AuditLog
                    {
                        EventType = AuditEventType.MilestoneUpcomingNotificationSent,
                        ProjectId = project.Id,
                        EmployeeId = project.ManagerId,
                        Details = $"Milestone upcoming notification sent to {project.Manager.Email} for Milestone: {milestone.Title}",
                        OccurredAt = DateTime.UtcNow
                    }, ct);
                }
            }
        }
    }

    private async Task<ProjectHealthEvaluation> ComputeProjectHealthAsync(
        Domain.Entities.Project project, DateOnly today, DateOnly lastWeek,
        int maxWeeklyHours, CancellationToken ct)
    {
        bool isAtRisk = false;
        bool needsAttention = false;
        var reasons = new List<string>();

        // Check overdue milestones
        var overdueMilestones = project.Milestones
            .Where(m => m.Status == MilestoneStatus.InProgress && m.DueDate < today)
            .ToList();

        if (overdueMilestones.Any())
        {
            isAtRisk = true;
            reasons.Add($"Overdue milestones: {string.Join(", ", overdueMilestones.Select(m => m.Title))}.");
        }

        // Check milestones due within 7 days and not started
        var upcomingNotStarted = project.Milestones
            .Where(m => m.Status == MilestoneStatus.NotStarted && m.DueDate <= today.AddDays(7))
            .ToList();

        if (upcomingNotStarted.Any())
        {
            needsAttention = true;
            reasons.Add($"Upcoming milestones not started: {string.Join(", ", upcomingNotStarted.Select(m => m.Title))}.");
        }

        // Check effort logged last week
        var activeAllocations = project.Allocations
            .Where(a => a.IsActive && a.FromDate <= lastWeek && a.ToDate >= lastWeek)
            .ToList();

        if (activeAllocations.Any())
        {
            decimal totalExpected = activeAllocations.Sum(a => a.UtilisationPercent / 100m * maxWeeklyHours);
            decimal totalActual = 0;
            var projectTimesheets = await _timesheets.GetByProjectAndWeekAsync(project.Id, lastWeek, ct);

            foreach (var alloc in activeAllocations)
            {
                var empActual = projectTimesheets
                    .Where(t => t.UserId == alloc.UserId)
                    .Sum(t => t.TotalHoursWorked);

                // If any employee logged < 50% expected, mark AT RISK
                var empExpected = alloc.UtilisationPercent / 100m * maxWeeklyHours;
                if (empExpected > 0 && empActual < empExpected * 0.5m)
                {
                    isAtRisk = true;
                    reasons.Add($"{alloc.User?.FullName ?? $"Employee {alloc.UserId}"} logged {empActual:0.##}h against expected {empExpected:0.##}h.");
                }

                totalActual += empActual;
            }

            // If total < 70% of expected, mark ATTENTION
            if (totalExpected > 0 && totalActual < totalExpected * 0.7m)
            {
                needsAttention = true;
                reasons.Add($"Total logged hours {totalActual:0.##}h are below expected {totalExpected:0.##}h.");
            }
        }

        var status = ProjectHealthStatus.OnTrack;
        if (isAtRisk) status = ProjectHealthStatus.AtRisk;
        else if (needsAttention) status = ProjectHealthStatus.Attention;

        if (!reasons.Any())
            reasons.Add("Project health changed based on latest milestone and effort evaluation.");

        return new ProjectHealthEvaluation(status, reasons);
    }

    private static DateOnly GetPreviousWeekStart(DateOnly today)
    {
        var offset = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var currentWeekStart = today.AddDays(-offset);
        return currentWeekStart.AddDays(-7);
    }

    private static Application.DTOs.Notification.ProjectRiskNotificationContextDto BuildRiskNotificationContext(
        Domain.Entities.Project project,
        IReadOnlyCollection<Domain.Entities.User> employees,
        DateOnly today,
        ProjectHealthEvaluation evaluation)
    {
        var activeProjectUserIds = project.Allocations
            .Where(a => a.IsActive && a.FromDate <= today && a.ToDate >= today)
            .Select(a => a.UserId)
            .ToHashSet();

        var projectSkills = project.Allocations
            .SelectMany(a => a.User?.Skills ?? Enumerable.Empty<Domain.Entities.UserSkill>())
            .Select(s => s.Skill?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var recommendations = employees
            .Where(e => e.IsActive && !activeProjectUserIds.Contains(e.Id))
            .Select(e => new
            {
                Employee = e,
                ActiveAllocation = e.Allocations.Any(a => a.IsActive && a.FromDate <= today && a.ToDate >= today),
                MatchingSkills = e.Skills
                    .Select(s => s.Skill?.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name) && projectSkills.Contains(name!))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .Where(x => !x.ActiveAllocation && x.MatchingSkills.Any())
            .OrderByDescending(x => x.MatchingSkills.Count)
            .ThenBy(x => x.Employee.FullName)
            .Take(5)
            .Select(x => $"{x.Employee.FullName} ({x.Employee.Email}) - matching skills: {string.Join(", ", x.MatchingSkills)}")
            .ToList();

        var milestones = project.Milestones
            .OrderBy(m => m.DueDate)
            .Take(5)
            .Select(m => $"{m.Title} ({m.Status}, due {m.DueDate:yyyy-MM-dd})")
            .ToList();

        var suggestedHelp = new List<string>
        {
            "Review overdue milestones and re-plan the next delivery window.",
            "Rebalance team capacity against expected weekly hours.",
            recommendations.Any()
                ? "Consider assigning one of the recommended available employees."
                : "Review staffing options because no currently available matching employees were found."
        };

        return new Application.DTOs.Notification.ProjectRiskNotificationContextDto(
            project.Id,
            project.Name,
            project.Manager.FullName,
            project.Manager.Email,
            MapHealthStatus(project.HealthStatus),
            "High",
            string.Join(" ", evaluation.Reasons),
            milestones,
            suggestedHelp,
            recommendations.Any() ? recommendations : new[] { "No available matching employees found." });
    }

    private static string MapHealthStatus(ProjectHealthStatus status)
        => status switch
        {
            ProjectHealthStatus.OnTrack => "Green",
            ProjectHealthStatus.Attention => "Amber",
            ProjectHealthStatus.AtRisk => "Red",
            _ => status.ToString()
        };

    private sealed record ProjectHealthEvaluation(ProjectHealthStatus Status, IReadOnlyCollection<string> Reasons);
}
