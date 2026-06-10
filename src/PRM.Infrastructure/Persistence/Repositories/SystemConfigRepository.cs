using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.Persistence.Repositories;

public class SystemConfigRepository : ISystemConfigRepository
{
    private readonly PrmDbContext _db;

    public SystemConfigRepository(PrmDbContext db) => _db = db;

    public async Task<SystemConfig> GetAsync(CancellationToken ct = default)
    {
        var config = await _db.SystemConfigs.FirstOrDefaultAsync(ct);
        return config ?? throw new EntityNotFoundException("System configuration not found.");
    }

    public async Task UpdateAsync(SystemConfig config, CancellationToken ct = default)
    {
        _db.SystemConfigs.Update(config);
        await _db.SaveChangesAsync(ct);
    }
}
