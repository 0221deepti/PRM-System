using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

public interface IProjectRepository : IRepository<Project>
{
    Task<IEnumerable<Project>> GetByManagerAsync(int managerId, CancellationToken ct = default);
    Task<Project?> GetWithMilestonesAsync(int projectId, CancellationToken ct = default);
    Task<Project?> GetWithAllocationsAsync(int projectId, CancellationToken ct = default);
    Task<IEnumerable<Project>> GetAllWithDetailsAsync(CancellationToken ct = default);
}
