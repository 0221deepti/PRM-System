using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

public class TimesheetRepository : Repository<Timesheet>, ITimesheetRepository
{
    public TimesheetRepository(PrmDbContext db) : base(db) { }

    public async Task<IEnumerable<Timesheet>> GetByUserAsync(int userId, CancellationToken ct = default)
        => await _set
            .Include(t => t.Project)
            .Include(t => t.Entries).ThenInclude(e => e.Tags)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.WeekStartDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<Timesheet>> GetByWeekAsync(DateOnly weekStart, int managerId, CancellationToken ct = default)
        => await _set
            .Include(t => t.User)
            .Include(t => t.Project)
            .Include(t => t.Entries)
            .Where(t => t.WeekStartDate == weekStart && t.User.ManagerId == managerId)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(int userId, int projectId, DateOnly weekStart, CancellationToken ct = default)
        => await _set.AnyAsync(t => t.UserId == userId
                                 && t.ProjectId == projectId
                                 && t.WeekStartDate == weekStart, ct);

    public async Task<decimal> GetTotalHoursForWeekAsync(int userId, DateOnly weekStart, CancellationToken ct = default)
        => await _set
            .Where(t => t.UserId == userId && t.WeekStartDate == weekStart)
            .SumAsync(t => t.TotalHoursWorked, ct);

    public async Task<IEnumerable<Timesheet>> GetByProjectAndWeekAsync(int projectId, DateOnly weekStart, CancellationToken ct = default)
        => await _set
            .Include(t => t.User)
            .Include(t => t.Entries).ThenInclude(e => e.Tags)
            .Where(t => t.ProjectId == projectId && t.WeekStartDate == weekStart)
            .ToListAsync(ct);

    public async Task<Timesheet?> GetWithEntriesAsync(int timesheetId, CancellationToken ct = default)
        => await _set
            .Include(t => t.Entries).ThenInclude(e => e.Tags).ThenInclude(t => t.ActivityTag)
            .FirstOrDefaultAsync(t => t.Id == timesheetId, ct);
}
