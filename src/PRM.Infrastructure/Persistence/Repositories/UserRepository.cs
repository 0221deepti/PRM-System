using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(PrmDbContext db) : base(db) { }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _set
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _set
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default)
        => await _set.AnyAsync(u => u.Username == username || u.Email == email, ct);

    public async Task<IEnumerable<User>> GetByManagerIdAsync(int managerId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Role)
            .Include(u => u.Skills).ThenInclude(s => s.Skill)
            .Include(u => u.Allocations)
            .Where(u => u.ManagerId == managerId && u.IsActive)
            .ToListAsync(ct);

    public async Task<IEnumerable<User>> GetBenchUsersAsync(int managerId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Role)
            .Include(u => u.Skills).ThenInclude(s => s.Skill)
            .Where(u => u.ManagerId == managerId && u.Status == EmployeeStatus.Bench && u.IsActive)
            .ToListAsync(ct);

    public async Task<User?> GetWithSkillsAsync(int userId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Role)
            .Include(u => u.Skills).ThenInclude(s => s.Skill)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    public async Task<User?> GetWithAllocationsAsync(int userId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Role)
            .Include(u => u.Allocations).ThenInclude(a => a.Project)
            .Include(u => u.Skills).ThenInclude(s => s.Skill)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    public async Task<IEnumerable<User>> GetAllWithDetailsAsync(CancellationToken ct = default)
        => await _set
            .Include(u => u.Role)
            .Include(u => u.Skills).ThenInclude(s => s.Skill)
            .ToListAsync(ct);

    public async Task<User?> GetWithRoleAndPermissionsAsync(int userId, CancellationToken ct = default)
        => await _set
            .Include(u => u.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
}
