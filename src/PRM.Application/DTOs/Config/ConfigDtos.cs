namespace PRM.Application.DTOs.Config;

public record SystemConfigDto(
    string LlmProvider,
    string LlmApiKeyMasked,
    int SchedulerIntervalHours,
    int MaxWeeklyHours);

public record UpdateConfigDto(
    string? LlmProvider,
    string? LlmApiKey,
    int? SchedulerIntervalHours,
    int? MaxWeeklyHours);
