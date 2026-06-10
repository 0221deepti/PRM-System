using PRM.Client.UI;

namespace PRM.Client.UI.Screens;

public class AdminMenuScreen : Screen
{
    public AdminMenuScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        await Task.CompletedTask;
        ShowHeader("Admin Dashboard");

        Console.WriteLine("1. Manage Users");
        Console.WriteLine("2. Manage Employees & Skills");
        Console.WriteLine("3. Manage Projects");
        Console.WriteLine("4. View All Allocations");
        Console.WriteLine("5. System Configuration");
        Console.WriteLine("0. Back to Main Menu");

        var choice = InputHelper.ReadString("Select an option");

        switch (choice)
        {
            case "1":
                ConsoleRenderer.RenderWarning("Manage Users screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "2":
                ConsoleRenderer.RenderWarning("Manage Employees screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "3":
                ConsoleRenderer.RenderWarning("Manage Projects screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "4":
                ConsoleRenderer.RenderWarning("View Allocations screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "5":
                ConsoleRenderer.RenderWarning("System Configuration screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "0":
                return false; // go back
            default:
                ConsoleRenderer.RenderError("Invalid option.");
                ConsoleRenderer.Pause();
                break;
        }

        return true; // stay on admin menu
    }
}
