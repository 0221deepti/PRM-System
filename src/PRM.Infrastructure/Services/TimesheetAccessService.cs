using PRM.Application.DTOs.Notification;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.Services;

public class TimesheetAccessService : ITimesheetAccessService
{
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;
    private readonly IAllocationRepository _allocations;
    private readonly ITimesheetRepository _timesheets;
    private readonly IEmployeeAccessStatusRepository _accessStatuses;
    private readonly IEmailService _emailService;
    private readonly IRepository<AuditLog> _auditLogs;

    public TimesheetAccessService(
        IEmployeeRepository employees,
        IUserRepository users,
        IAllocationRepository allocations,
        ITimesheetRepository timesheets,
        IEmployeeAccessStatusRepository accessStatuses,
        IEmailService emailService,
        IRepository<AuditLog> auditLogs)
    {
        _employees = employees;
        _users = users;
        _allocations = allocations;
        _timesheets = timesheets;
        _accessStatuses = accessStatuses;
        _emailService = emailService;
        _auditLogs = auditLogs;
    }

    public async Task EnsureTimesheetAccessAsync(int employeeId, CancellationToken ct)
    {
        var frozenStatus = await _accessStatuses.GetFrozenStatusAsync(employeeId, ct);
        if (frozenStatus?.IsTimesheetFrozen == true)
            throw new DomainException("Timesheet access is currently frozen. Please contact your reporting manager to restore access.");
    }

    public async Task<TimesheetAccessStatusDto?> GetCurrentStatusAsync(int employeeId, int callerUserId, string callerRole, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(employeeId, ct);
        if (employee == null) return null;

        if (callerRole == "Manager" && employee.ManagerId != callerUserId)
            throw new DomainException("This employee is not assigned to your team.");

        if (callerRole == "Employee" && employee.Id != callerUserId)
            throw new PrmUnauthorizedException("Unauthorized to view this access status.");

        var status = await _accessStatuses.GetLatestForEmployeeAsync(employeeId, ct);
        return status == null ? null : Map(status);
    }

    public async Task<TimesheetAccessStatusDto> RestoreAccessAsync(int managerId, int employeeId, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(employeeId, ct)
            ?? throw new EntityNotFoundException("Employee not found.");

        if (employee.ManagerId != managerId)
            throw new DomainException("This employee is not assigned to your team.");

        var status = await _accessStatuses.GetFrozenStatusAsync(employeeId, ct)
            ?? throw new DomainException("Timesheet access is not frozen for this employee.");

        status.IsTimesheetFrozen = false;
        status.RestoredDate = DateTime.UtcNow;
        status.RestoredBy = managerId;
        _accessStatuses.Update(status);

        await _auditLogs.AddAsync(new AuditLog
        {
            EventType = AuditEventType.AccountRestored,
            EmployeeId = employeeId,
            PerformedByUserId = managerId,
            Details = $"Timesheet access restored for employee {employeeId}.",
            OccurredAt = DateTime.UtcNow
        }, ct);

        await _accessStatuses.SaveChangesAsync(ct);

        var manager = await _users.GetByIdAsync(managerId, ct);
        await SendRestoreNotificationsAsync(employee, manager, status.TrackedWeekStartDate, ct);

        return Map(status);
    }

    public async Task ProcessDailyAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!IsWorkingDay(today))
            return;

        var trackedWeekStart = GetPreviousWeekStart(today);
        var submissionDeadline = GetNextWorkingDay(trackedWeekStart.AddDays(6));
        var reminder1Date = GetNextWorkingDay(submissionDeadline);
        var reminder2Date = GetNextWorkingDay(reminder1Date);
        var freezeDate = GetNextWorkingDay(reminder2Date);

        var employees = await _employees.GetAllWithDetailsAsync(ct);

        foreach (var employee in employees.Where(x => x.IsActive))
        {
            var allocations = await _allocations.GetActiveByUserAsync(employee.Id, ct);
            var activeLastWeek = allocations
                .Where(a => a.IsActive && a.FromDate <= trackedWeekStart && a.ToDate >= trackedWeekStart)
                .ToList();

            if (!activeLastWeek.Any())
                continue;

            var timesheets = await _timesheets.GetByUserAsync(employee.Id, ct);
            var submittedProjectIds = timesheets
                .Where(t => t.WeekStartDate == trackedWeekStart)
                .Select(t => t.ProjectId)
                .ToHashSet();

            if (activeLastWeek.All(a => submittedProjectIds.Contains(a.ProjectId)))
                continue;

            var status = await _accessStatuses.GetByEmployeeAndWeekAsync(employee.Id, trackedWeekStart, ct);
            var isNewStatus = status == null;

            status ??= new EmployeeAccessStatus
            {
                EmployeeId = employee.Id,
                TrackedWeekStartDate = trackedWeekStart
            };

            if (status.RestoredDate.HasValue)
                continue;

            var manager = employee.ManagerId.HasValue
                ? await _users.GetByIdAsync(employee.ManagerId.Value, ct)
                : null;

            if (status.Reminder1SentDate == null && today >= reminder1Date)
            {
                var sent = await SendReminderAsync(
                    NotificationTemplateNames.TimesheetReminder1,
                    NotificationType.TimesheetReminder1,
                    AuditEventType.Reminder1Sent,
                    employee,
                    manager,
                    trackedWeekStart,
                    submissionDeadline,
                    ct);

                if (sent)
                    status.Reminder1SentDate = DateTime.UtcNow;
            }
            else if (status.Reminder2SentDate == null && status.Reminder1SentDate != null && today >= reminder2Date)
            {
                var sent = await SendReminderAsync(
                    NotificationTemplateNames.TimesheetReminder2,
                    NotificationType.TimesheetReminder2,
                    AuditEventType.Reminder2Sent,
                    employee,
                    manager,
                    trackedWeekStart,
                    submissionDeadline,
                    ct);

                if (sent)
                    status.Reminder2SentDate = DateTime.UtcNow;
            }
            else if (!status.IsTimesheetFrozen && status.Reminder2SentDate != null && today >= freezeDate)
            {
                status.IsTimesheetFrozen = true;
                status.FreezeDate = DateTime.UtcNow;

                await SendFreezeNotificationsAsync(employee, manager, trackedWeekStart, ct);

                await _auditLogs.AddAsync(new AuditLog
                {
                    EventType = AuditEventType.AccountFrozen,
                    EmployeeId = employee.Id,
                    Details = $"Timesheet access frozen for employee {employee.Id} for week {trackedWeekStart:yyyy-MM-dd}.",
                    OccurredAt = DateTime.UtcNow
                }, ct);
            }

            if (isNewStatus)
                await _accessStatuses.AddAsync(status, ct);
            else
                _accessStatuses.Update(status);

            await _accessStatuses.SaveChangesAsync(ct);
        }
    }

    private async Task<bool> SendReminderAsync(
        string templateName,
        NotificationType notificationType,
        AuditEventType auditEventType,
        User employee,
        User? manager,
        DateOnly trackedWeekStart,
        DateOnly submissionDeadline,
        CancellationToken ct)
    {
        var result = await _emailService.SendTemplateEmailAsync(
            templateName,
            employee.Email,
            BuildPlaceholders(employee, manager, trackedWeekStart, submissionDeadline),
            ct);

        if (!result.IsSuccess)
            return false;

        await _auditLogs.AddAsync(new AuditLog
        {
            EventType = auditEventType,
            EmployeeId = employee.Id,
            Details = $"{notificationType} sent to {employee.Email}.",
            OccurredAt = DateTime.UtcNow
        }, ct);

        return true;
    }

    private async Task SendFreezeNotificationsAsync(User employee, User? manager, DateOnly trackedWeekStart, CancellationToken ct)
    {
        var placeholders = BuildPlaceholders(employee, manager, trackedWeekStart, GetNextWorkingDay(trackedWeekStart.AddDays(6)));

        await _emailService.SendTemplateEmailAsync(
            NotificationTemplateNames.AccountFreezeNotification,
            employee.Email,
            placeholders,
            ct);

        if (!string.IsNullOrWhiteSpace(manager?.Email))
        {
            await _emailService.SendTemplateEmailAsync(
                NotificationTemplateNames.AccountFreezeNotification,
                manager.Email,
                placeholders,
                ct);
        }
    }

    private async Task SendRestoreNotificationsAsync(User employee, User? manager, DateOnly trackedWeekStart, CancellationToken ct)
    {
        var placeholders = BuildPlaceholders(employee, manager, trackedWeekStart, GetNextWorkingDay(trackedWeekStart.AddDays(6)));

        await _emailService.SendTemplateEmailAsync(
            NotificationTemplateNames.AccountRestoreNotification,
            employee.Email,
            placeholders,
            ct);

        if (!string.IsNullOrWhiteSpace(manager?.Email))
        {
            await _emailService.SendTemplateEmailAsync(
                NotificationTemplateNames.AccountRestoreNotification,
                manager.Email,
                placeholders,
                ct);
        }
    }

    private static Dictionary<string, string> BuildPlaceholders(
        User employee,
        User? manager,
        DateOnly trackedWeekStart,
        DateOnly submissionDeadline)
        => new()
        {
            ["EmployeeName"] = employee.FullName,
            ["EmployeeEmail"] = employee.Email,
            ["ManagerName"] = manager?.FullName ?? "Reporting Manager",
            ["WeekStartDate"] = trackedWeekStart.ToString("yyyy-MM-dd"),
            ["SubmissionDeadline"] = submissionDeadline.ToString("yyyy-MM-dd")
        };

    private static TimesheetAccessStatusDto Map(EmployeeAccessStatus status)
        => new(
            status.EmployeeId,
            status.TrackedWeekStartDate,
            status.IsTimesheetFrozen,
            status.Reminder1SentDate,
            status.Reminder2SentDate,
            status.FreezeDate,
            status.RestoredDate,
            status.RestoredBy);

    private static bool IsWorkingDay(DateOnly date)
        => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    private static DateOnly GetPreviousWeekStart(DateOnly today)
    {
        var offset = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var currentWeekStart = today.AddDays(-offset);
        return currentWeekStart.AddDays(-7);
    }

    private static DateOnly GetNextWorkingDay(DateOnly date)
    {
        var next = date.AddDays(1);
        while (!IsWorkingDay(next))
            next = next.AddDays(1);

        return next;
    }
}