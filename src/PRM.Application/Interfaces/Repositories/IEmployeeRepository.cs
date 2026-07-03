using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

/// <summary>
/// Employee repository - provides specialized queries for employee/user management.
/// After refactoring, operates on User entity which consolidates Employee and User.
/// </summary>
public interface IEmployeeRepository : IRepository<User>
{
    /// <summary>Get team members managed by a specific user</summary>
    Task<IEnumerable<User>> GetByManagerIdAsync(int managerId, CancellationToken ct = default);

    /// <summary>Get bench (unallocated) employees for a manager</summary>
    Task<IEnumerable<User>> GetBenchEmployeesAsync(int managerId, CancellationToken ct = default);

    /// <summary>Get employee with all skills loaded</summary>
    Task<User?> GetWithSkillsAsync(int employeeId, CancellationToken ct = default);

    /// <summary>Get employee with all allocations loaded</summary>
    Task<User?> GetWithAllocationsAsync(int employeeId, CancellationToken ct = default);

    /// <summary>Get employee by associated user ID</summary>
    Task<User?> GetByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>Get all employees with full details (manager, skills, allocations)</summary>
    Task<IEnumerable<User>> GetAllWithDetailsAsync(CancellationToken ct = default);
}
