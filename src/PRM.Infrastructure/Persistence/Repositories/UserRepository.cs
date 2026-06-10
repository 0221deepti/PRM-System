using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(PrmDbContext db) : base(db) { }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> ExistsAsync(string username, string email, CancellationToken ct = default)
        => await _set.AnyAsync(u => u.Username == username || u.Email == email, ct);
}
