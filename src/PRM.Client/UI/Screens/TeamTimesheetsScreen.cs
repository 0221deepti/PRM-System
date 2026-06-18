using PRM.Client.UI;
using PRM.Application.DTOs.Timesheet;

namespace PRM.Client.UI.Screens;

public class TeamTimesheetsScreen : Screen
{
    public TeamTimesheetsScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Team Timesheets");
        var week = InputHelper.ReadDate("Week Start Date (for team view)");
        var list = await _services.Timesheets.GetTeamAsync(week);
        Console.WriteLine($"Team Timesheets for week starting {week}:");
        foreach (var t in list)
        {
            Console.WriteLine($"{t.EmployeeName} - {t.ProjectName} - {t.HoursWorked}h - Submitted: {t.IsSubmitted}");
        }

        Console.WriteLine();
        Console.WriteLine("0. Back");
        var _ = InputHelper.ReadString("Press 0 to go back", required: false);
        return false;
    }
}
