using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default);
    Task<IEnumerable<User>> GetByManagerIdAsync(int managerId, CancellationToken ct = default);
    Task<IEnumerable<User>> GetBenchUsersAsync(int managerId, CancellationToken ct = default);
    Task<User?> GetWithSkillsAsync(int userId, CancellationToken ct = default);
    Task<User?> GetWithAllocationsAsync(int userId, CancellationToken ct = default);
    Task<IEnumerable<User>> GetAllWithDetailsAsync(CancellationToken ct = default);
    Task<User?> GetWithRoleAndPermissionsAsync(int userId, CancellationToken ct = default);
}
