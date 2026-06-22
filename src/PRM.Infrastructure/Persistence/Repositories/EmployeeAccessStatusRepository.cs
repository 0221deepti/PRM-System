using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Repositories;

public class EmployeeAccessStatusRepository : Repository<EmployeeAccessStatus>, IEmployeeAccessStatusRepository
{
    public EmployeeAccessStatusRepository(PrmDbContext db) : base(db) { }

    public async Task<EmployeeAccessStatus?> GetByEmployeeAndWeekAsync(int employeeId, DateOnly trackedWeekStartDate, CancellationToken ct = default)
        => await _set
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.TrackedWeekStartDate == trackedWeekStartDate, ct);

    public async Task<EmployeeAccessStatus?> GetLatestForEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.TrackedWeekStartDate)
            .FirstOrDefaultAsync(ct);

    public async Task<EmployeeAccessStatus?> GetFrozenStatusAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .Where(x => x.EmployeeId == employeeId && x.IsTimesheetFrozen)
            .OrderByDescending(x => x.TrackedWeekStartDate)
            .FirstOrDefaultAsync(ct);
}