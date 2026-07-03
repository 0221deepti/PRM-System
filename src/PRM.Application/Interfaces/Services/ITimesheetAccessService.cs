using PRM.Application.DTOs.Notification;

namespace PRM.Application.Interfaces.Services;

public interface ITimesheetAccessService
{
    Task EnsureTimesheetAccessAsync(int employeeId, CancellationToken ct);
    Task<TimesheetAccessStatusDto?> GetCurrentStatusAsync(int employeeId, int callerUserId, string callerRole, CancellationToken ct);
    Task<TimesheetAccessStatusDto> RestoreAccessAsync(int managerId, int employeeId, CancellationToken ct);
    Task ProcessDailyAsync(CancellationToken ct);
}