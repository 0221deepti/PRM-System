using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(PrmDbContext db) : base(db) { }

    public async Task<IEnumerable<Project>> GetByManagerAsync(int managerId, CancellationToken ct = default)
        => await _set
            .Include(p => p.Milestones)
            .Where(p => p.ManagerId == managerId)
            .ToListAsync(ct);

    public async Task<Project?> GetWithMilestonesAsync(int projectId, CancellationToken ct = default)
        => await _set
            .Include(p => p.Milestones)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

    public async Task<Project?> GetWithAllocationsAsync(int projectId, CancellationToken ct = default)
        => await _set
            .Include(p => p.Milestones)
            .Include(p => p.Allocations).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

    public async Task<IEnumerable<Project>> GetAllWithDetailsAsync(CancellationToken ct = default)
        => await _set
            .Include(p => p.Manager)
            .Include(p => p.Milestones)
            .ToListAsync(ct);
}
