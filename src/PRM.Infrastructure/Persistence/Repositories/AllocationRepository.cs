using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

public class AllocationRepository : Repository<Allocation>, IAllocationRepository
{
    public AllocationRepository(PrmDbContext db) : base(db) { }

    public async Task<IEnumerable<Allocation>> GetActiveByUserAsync(int userId, CancellationToken ct = default)
        => await _set
            .Include(a => a.Project)
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync(ct);

    public async Task<IEnumerable<Allocation>> GetActiveByProjectAsync(int projectId, CancellationToken ct = default)
        => await _set
            .Include(a => a.User)
            .Where(a => a.ProjectId == projectId && a.IsActive)
            .ToListAsync(ct);

    public async Task<int> GetTotalUtilisationAsync(
        int userId, DateOnly from, DateOnly to,
        int? excludeAllocationId = null,
        CancellationToken ct = default)
    {
        return await _set
            .Where(a => a.UserId == userId
                     && a.IsActive
                     && a.FromDate <= to
                     && a.ToDate >= from
                     && (excludeAllocationId == null || a.Id != excludeAllocationId))
            .SumAsync(a => a.UtilisationPercent, ct);
    }

    public async Task<IEnumerable<Allocation>> GetAllActiveWithDetailsAsync(CancellationToken ct = default)
        => await _set
            .Include(a => a.User)
            .Include(a => a.Project)
            .Where(a => a.IsActive)
            .ToListAsync(ct);
}
