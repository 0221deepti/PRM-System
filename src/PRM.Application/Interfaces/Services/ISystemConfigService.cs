using PRM.Application.DTOs.Config;

namespace PRM.Application.Interfaces.Services;

public interface ISystemConfigService
{
    Task<SystemConfigDto> GetConfigAsync(CancellationToken ct);
    Task UpdateConfigAsync(UpdateConfigDto dto, CancellationToken ct);
}
