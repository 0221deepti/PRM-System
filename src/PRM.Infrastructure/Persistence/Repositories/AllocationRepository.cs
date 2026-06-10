using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

public class AllocationRepository : Repository<Allocation>, IAllocationRepository
{
    public AllocationRepository(PrmDbContext db) : base(db) { }

    public async Task<IEnumerable<Allocation>> GetActiveByEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .Include(a => a.Project)
            .Where(a => a.EmployeeId == employeeId && a.IsActive)
            .ToListAsync(ct);

    public async Task<IEnumerable<Allocation>> GetActiveByProjectAsync(int projectId, CancellationToken ct = default)
        => await _set
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Where(a => a.ProjectId == projectId && a.IsActive)
            .ToListAsync(ct);

    public async Task<int> GetTotalUtilisationAsync(
        int employeeId, DateOnly from, DateOnly to,
        int? excludeAllocationId = null,
        CancellationToken ct = default)
    {
        return await _set
            .Where(a => a.EmployeeId == employeeId
                     && a.IsActive
                     && a.FromDate <= to
                     && a.ToDate >= from
                     && (excludeAllocationId == null || a.Id != excludeAllocationId))
            .SumAsync(a => a.UtilisationPercent, ct);
    }

    public async Task<IEnumerable<Allocation>> GetAllActiveWithDetailsAsync(CancellationToken ct = default)
        => await _set
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Include(a => a.Project)
            .Where(a => a.IsActive)
            .ToListAsync(ct);
}
