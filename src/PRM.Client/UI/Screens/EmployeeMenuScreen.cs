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

        var choice = InputHelper.ReadString("Select an option");

        switch (choice)
        {
            case "1":
                ConsoleRenderer.RenderWarning("My Profile screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "2":
                ConsoleRenderer.RenderWarning("My Allocations screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "3":
                ConsoleRenderer.RenderWarning("Submit Timesheet screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "4":
                ConsoleRenderer.RenderWarning("My Timesheets screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "0":
                return false;
            default:
                ConsoleRenderer.RenderError("Invalid option.");
                ConsoleRenderer.Pause();
                break;
        }

        return true;
    }
}
