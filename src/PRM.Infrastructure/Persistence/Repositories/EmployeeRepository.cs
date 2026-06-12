using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

/// <summary>
/// Employee repository - after refactoring, this works with User entity.
/// Provides specialized queries for employee/team management.
/// </summary>
public class EmployeeRepository : Repository<User>, IEmployeeRepository
{
    public EmployeeRepository(PrmDbContext db) : base(db) { }

    /// <summary>Get team members managed by a specific user</summary>
    public async Task<IEnumerable<User>> GetByManagerIdAsync(int managerId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Skills).ThenInclude(us => us.Skill)
            .Include(u => u.Allocations)
            .Include(u => u.Role)
            .Where(u => u.ManagerId == managerId && u.IsActive)
            .ToListAsync(ct);

    /// <summary>Get bench (unallocated) employees for a manager</summary>
    public async Task<IEnumerable<User>> GetBenchEmployeesAsync(int managerId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Skills).ThenInclude(us => us.Skill)
            .Include(u => u.Role)
            .Where(u => u.ManagerId == managerId && u.Status == EmployeeStatus.Bench && u.IsActive)
            .ToListAsync(ct);

    /// <summary>Get employee with all skills loaded</summary>
    public async Task<User?> GetWithSkillsAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Skills).ThenInclude(us => us.Skill)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == employeeId, ct);

    /// <summary>Get employee with all allocations loaded</summary>
    public async Task<User?> GetWithAllocationsAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Allocations).ThenInclude(a => a.Project)
            .Include(u => u.Skills).ThenInclude(us => us.Skill)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == employeeId, ct);

    /// <summary>Get employee by associated user ID (legacy - now same as GetByIdAsync)</summary>
    public async Task<User?> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Skills).ThenInclude(us => us.Skill)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    /// <summary>Get all employees with full details</summary>
    public async Task<IEnumerable<User>> GetAllWithDetailsAsync(CancellationToken ct = default)
        => await _set
            .Include(u => u.Skills).ThenInclude(us => us.Skill)
            .Include(u => u.Allocations)
            .Include(u => u.Role)
            .ToListAsync(ct);
}
