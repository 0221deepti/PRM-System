namespace PRM.Domain.Entities;

public class SystemConfig : BaseEntity
{
    public string LlmProvider { get; set; } = "LocalGemma";
    public string LlmApiKey { get; set; } = "8e6aNK83g9YFBk1fNSW60eukXKSvOoZJ";
    public string LlmApiUrl { get; set; } = "http://164.52.211.238/api/generate";
    public string LlmModelName { get; set; } = "gemma3:12b-it-q8_0";
    public int SchedulerIntervalHours { get; set; } = 4;
    public int MaxWeeklyHours { get; set; } = 40;
}
