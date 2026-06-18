namespace PRM.Domain.Entities;

public class SystemConfig : BaseEntity
{
    public string LlmProvider { get; set; } = "Gemini";
    public string LlmApiKey { get; set; } = string.Empty;
    public int SchedulerIntervalHours { get; set; } = 4;
    public int MaxWeeklyHours { get; set; } = 40;
}
