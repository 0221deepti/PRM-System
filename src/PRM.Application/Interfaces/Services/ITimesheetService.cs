using PRM.Application.DTOs.Timesheet;

namespace PRM.Application.Interfaces.Services;

public interface ITimesheetService
{
    Task SubmitAsync(SubmitTimesheetDto dto, int employeeId, CancellationToken ct);
    Task<IEnumerable<TimesheetSummaryDto>> GetMyTimesheetsAsync(int employeeId, CancellationToken ct);
    Task<IEnumerable<TeamTimesheetEntryDto>> GetTeamTimesheetsAsync(int managerId, DateOnly weekStart, CancellationToken ct);
    Task<bool> HasMissedLastWeekAsync(int employeeId, CancellationToken ct);
}
