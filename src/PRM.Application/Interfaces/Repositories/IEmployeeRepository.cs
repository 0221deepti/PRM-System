using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<IEnumerable<Employee>> GetByManagerIdAsync(int managerId, CancellationToken ct = default);
    Task<IEnumerable<Employee>> GetBenchEmployeesAsync(int managerId, CancellationToken ct = default);
    Task<Employee?> GetWithSkillsAsync(int employeeId, CancellationToken ct = default);
    Task<Employee?> GetWithAllocationsAsync(int employeeId, CancellationToken ct = default);
    Task<Employee?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<IEnumerable<Employee>> GetAllWithDetailsAsync(CancellationToken ct = default);
}
