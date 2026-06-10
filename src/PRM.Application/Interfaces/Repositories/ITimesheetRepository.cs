using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

public interface ITimesheetRepository : IRepository<Timesheet>
{
    Task<IEnumerable<Timesheet>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);
    Task<IEnumerable<Timesheet>> GetByWeekAsync(DateOnly weekStart, int managerId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int employeeId, int projectId, DateOnly weekStart, CancellationToken ct = default);
    Task<decimal> GetTotalHoursForWeekAsync(int employeeId, DateOnly weekStart, CancellationToken ct = default);
    Task<IEnumerable<Timesheet>> GetByProjectAndWeekAsync(int projectId, DateOnly weekStart, CancellationToken ct = default);
}
