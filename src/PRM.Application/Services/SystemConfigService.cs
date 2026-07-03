using PRM.Application.DTOs.Config;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;

namespace PRM.Application.Services;

public class SystemConfigService : ISystemConfigService
{
    private readonly ISystemConfigRepository _config;

    public SystemConfigService(ISystemConfigRepository config) => _config = config;

    public async Task<SystemConfigDto> GetConfigAsync(CancellationToken ct)
    {
        var config = await _config.GetAsync(ct);
        var masked = string.IsNullOrEmpty(config.LlmApiKey)
            ? "(not set)"
            : new string('*', Math.Min(config.LlmApiKey.Length, 28));

        return new SystemConfigDto(
            config.LlmProvider, 
            masked, 
            config.LlmApiUrl,
            config.LlmModelName,
            config.SchedulerIntervalHours, 
            config.MaxWeeklyHours);
    }

    public async Task UpdateConfigAsync(UpdateConfigDto dto, CancellationToken ct)
    {
        var config = await _config.GetAsync(ct);

        if (dto.LlmProvider != null) config.LlmProvider = dto.LlmProvider;
        if (dto.LlmApiKey != null) config.LlmApiKey = dto.LlmApiKey;
        if (dto.LlmApiUrl != null) config.LlmApiUrl = dto.LlmApiUrl;
        if (dto.LlmModelName != null) config.LlmModelName = dto.LlmModelName;
        if (dto.SchedulerIntervalHours.HasValue) config.SchedulerIntervalHours = dto.SchedulerIntervalHours.Value;
        if (dto.MaxWeeklyHours.HasValue) config.MaxWeeklyHours = dto.MaxWeeklyHours.Value;

        await _config.UpdateAsync(config, ct);
    }
}
