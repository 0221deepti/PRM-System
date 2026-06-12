using PRM.Client.UI;
using PRM.Application.DTOs.Config;

namespace PRM.Client.UI.Screens;

public class SystemConfigScreen : Screen
{
    public SystemConfigScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("System Configuration");
        var cfg = await _services.Config.GetConfigAsync();
        Console.WriteLine($"LLM Provider: {cfg.LlmProvider}");
        Console.WriteLine($"Scheduler Interval Hours: {cfg.SchedulerIntervalHours}");
        Console.WriteLine($"Max Weekly Hours: {cfg.MaxWeeklyHours}");

        Console.WriteLine();
        Console.WriteLine("0. Back");
        var _ = InputHelper.ReadString("Press 0 to go back", required: false);
        return false;
    }
}
