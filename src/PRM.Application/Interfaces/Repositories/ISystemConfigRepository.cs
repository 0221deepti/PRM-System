using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

public interface ISystemConfigRepository
{
    Task<SystemConfig> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(SystemConfig config, CancellationToken ct = default);
}
