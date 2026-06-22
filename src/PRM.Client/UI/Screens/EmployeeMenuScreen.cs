using PRM.Client.UI;

namespace PRM.Client.UI.Screens;

public class EmployeeMenuScreen : Screen
{
    public EmployeeMenuScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        await Task.CompletedTask;
        ShowHeader("Employee Dashboard");

        Console.WriteLine("1. View My Profile & Skills");
        Console.WriteLine("2. View My Allocations");
        Console.WriteLine("3. Submit Timesheet");
        Console.WriteLine("4. View My Timesheets");
        Console.WriteLine("0. Back to Main Menu");

        var choice = InputHelper.ReadMenuOption("Select an option", new[] { "1", "2", "3", "4", "0" });

        switch (choice)
        {
            case "1":
                await new MyProfileScreen(_services).RenderAsync();
                break;
            case "2":
                await new MyAllocationsScreen(_services).RenderAsync();
                break;
            case "3":
                await new SubmitTimesheetScreen(_services).RenderAsync();
                break;
            case "4":
                await new MyTimesheetsScreen(_services).RenderAsync();
                break;
            case "0":
                return false;
        }

        return true;
    }
}
