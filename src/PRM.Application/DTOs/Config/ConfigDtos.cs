using System.ComponentModel.DataAnnotations;

namespace PRM.Application.DTOs.Config;

public record SystemConfigDto(
    string LlmProvider,
    string LlmApiKeyMasked,
    string LlmApiUrl,
    string LlmModelName,
    int SchedulerIntervalHours,
    int MaxWeeklyHours);

public record UpdateConfigDto(
    [StringLength(100, ErrorMessage = "LLM provider name cannot exceed 100 characters.")] string? LlmProvider,
    [StringLength(256, ErrorMessage = "LLM API Key cannot exceed 256 characters.")] string? LlmApiKey,
    [StringLength(500, ErrorMessage = "LLM API URL cannot exceed 500 characters.")] string? LlmApiUrl,
    [StringLength(100, ErrorMessage = "LLM Model name cannot exceed 100 characters.")] string? LlmModelName,
    [Range(1, 168, ErrorMessage = "Scheduler interval hours must be between 1 and 168.")] int? SchedulerIntervalHours,
    [Range(1, 168, ErrorMessage = "Maximum weekly hours must be between 1 and 168.")] int? MaxWeeklyHours);
