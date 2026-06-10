using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

public interface IAllocationRepository : IRepository<Allocation>
{
    Task<IEnumerable<Allocation>> GetActiveByEmployeeAsync(int employeeId, CancellationToken ct = default);
    Task<IEnumerable<Allocation>> GetActiveByProjectAsync(int projectId, CancellationToken ct = default);
    Task<int> GetTotalUtilisationAsync(int employeeId, DateOnly from, DateOnly to, int? excludeAllocationId = null, CancellationToken ct = default);
    Task<IEnumerable<Allocation>> GetAllActiveWithDetailsAsync(CancellationToken ct = default);
}
