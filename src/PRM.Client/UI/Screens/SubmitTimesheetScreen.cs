using PRM.Client.UI;
using PRM.Application.DTOs.Timesheet;

namespace PRM.Client.UI.Screens;

public class SubmitTimesheetScreen : Screen
{
    public SubmitTimesheetScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Submit Timesheet");
        var projectId = InputHelper.ReadInt("Project Id");
        var week = InputHelper.ReadDate("Week Start Date");
        var hours = InputHelper.ReadDecimal("Hours Worked");
        Console.WriteLine("Enter comma-separated activity tags:");
        var tags = InputHelper.ReadString("Tags").Split(',').Select(s=>s.Trim()).Where(s=>!string.IsNullOrEmpty(s)).ToList();

        var dto = new SubmitTimesheetDto(projectId, week, hours, tags);
        await _services.Timesheets.SubmitAsync(dto);
        ConsoleRenderer.RenderSuccess("Timesheet submitted.");
        ConsoleRenderer.Pause();
        return false;
    }
}
