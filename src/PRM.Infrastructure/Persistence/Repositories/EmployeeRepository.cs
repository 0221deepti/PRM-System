using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(PrmDbContext db) : base(db) { }

    public async Task<IEnumerable<Employee>> GetByManagerIdAsync(int managerId, CancellationToken ct = default)
        => await _set
            .Include(e => e.User)
            .Include(e => e.Skills).ThenInclude(s => s.Skill)
            .Include(e => e.Allocations)
            .Where(e => e.ManagerId == managerId && e.User.IsActive)
            .ToListAsync(ct);

    public async Task<IEnumerable<Employee>> GetBenchEmployeesAsync(int managerId, CancellationToken ct = default)
        => await _set
            .Include(e => e.User)
            .Include(e => e.Skills).ThenInclude(s => s.Skill)
            .Where(e => e.ManagerId == managerId && e.Status == EmployeeStatus.Bench && e.User.IsActive)
            .ToListAsync(ct);

    public async Task<Employee?> GetWithSkillsAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .Include(e => e.User)
            .Include(e => e.Skills).ThenInclude(s => s.Skill)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);

    public async Task<Employee?> GetWithAllocationsAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .Include(e => e.User)
            .Include(e => e.Allocations).ThenInclude(a => a.Project)
            .Include(e => e.Skills).ThenInclude(s => s.Skill)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);

    public async Task<Employee?> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId, ct);

    public async Task<IEnumerable<Employee>> GetAllWithDetailsAsync(CancellationToken ct = default)
        => await _set
            .Include(e => e.User)
            .Include(e => e.Skills).ThenInclude(s => s.Skill)
            .ToListAsync(ct);
}
