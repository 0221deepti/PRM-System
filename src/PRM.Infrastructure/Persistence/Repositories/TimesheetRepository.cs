using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

public class TimesheetRepository : Repository<Timesheet>, ITimesheetRepository
{
    public TimesheetRepository(PrmDbContext db) : base(db) { }

    public async Task<IEnumerable<Timesheet>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .Include(t => t.Project)
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.WeekStartDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<Timesheet>> GetByWeekAsync(DateOnly weekStart, int managerId, CancellationToken ct = default)
        => await _set
            .Include(t => t.Employee).ThenInclude(e => e.User)
            .Include(t => t.Project)
            .Where(t => t.WeekStartDate == weekStart && t.Employee.ManagerId == managerId)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(int employeeId, int projectId, DateOnly weekStart, CancellationToken ct = default)
        => await _set.AnyAsync(t => t.EmployeeId == employeeId
                                 && t.ProjectId == projectId
                                 && t.WeekStartDate == weekStart, ct);

    public async Task<decimal> GetTotalHoursForWeekAsync(int employeeId, DateOnly weekStart, CancellationToken ct = default)
        => await _set
            .Where(t => t.EmployeeId == employeeId && t.WeekStartDate == weekStart)
            .SumAsync(t => t.HoursWorked, ct);

    public async Task<IEnumerable<Timesheet>> GetByProjectAndWeekAsync(int projectId, DateOnly weekStart, CancellationToken ct = default)
        => await _set
            .Include(t => t.Employee).ThenInclude(e => e.User)
            .Where(t => t.ProjectId == projectId && t.WeekStartDate == weekStart)
            .ToListAsync(ct);
}
