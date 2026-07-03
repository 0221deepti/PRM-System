using PRM.Client.UI;
using PRM.Application.DTOs.Timesheet;

namespace PRM.Client.UI.Screens;

public class MyTimesheetsScreen : Screen
{
    public MyTimesheetsScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("My Timesheets");
        var list = await _services.Timesheets.GetMineAsync();
        Console.WriteLine("My Timesheets:");
        foreach(var t in list)
            Console.WriteLine($"{t.Id}. {t.ProjectName} - Week {t.WeekStartDate} - {t.HoursWorked}h - Submitted: {t.IsSubmitted}");

        Console.WriteLine();
        Console.WriteLine("0. Back");
        var _ = InputHelper.ReadString("Press 0 to go back", required: false);
        return false;
    }
}
