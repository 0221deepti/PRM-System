namespace PRM.Application.DTOs.Config;

public record SystemConfigDto(
    string LlmProvider,
    string LlmApiKeyMasked,
    string LlmApiUrl,
    string LlmModelName,
    int SchedulerIntervalHours,
    int MaxWeeklyHours);

public record UpdateConfigDto(
    string? LlmProvider,
    string? LlmApiKey,
    string? LlmApiUrl,
    string? LlmModelName,
    int? SchedulerIntervalHours,
    int? MaxWeeklyHours);
