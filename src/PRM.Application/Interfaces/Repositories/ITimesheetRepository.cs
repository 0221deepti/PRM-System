using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

public interface ITimesheetRepository : IRepository<Timesheet>
{
    Task<IEnumerable<Timesheet>> GetByUserAsync(int userId, CancellationToken ct = default);
    Task<IEnumerable<Timesheet>> GetByWeekAsync(DateOnly weekStart, int managerId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int userId, int projectId, DateOnly weekStart, CancellationToken ct = default);
    Task<decimal> GetTotalHoursForWeekAsync(int userId, DateOnly weekStart, CancellationToken ct = default);
    Task<IEnumerable<Timesheet>> GetByProjectAndWeekAsync(int projectId, DateOnly weekStart, CancellationToken ct = default);
    Task<Timesheet?> GetWithEntriesAsync(int timesheetId, CancellationToken ct = default);
}
