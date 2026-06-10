using PRM.Client.UI;

namespace PRM.Client.UI.Screens;

public class ManagerMenuScreen : Screen
{
    public ManagerMenuScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        await Task.CompletedTask;
        ShowHeader("Manager Dashboard");

        Console.WriteLine("1. My Projects & Milestones");
        Console.WriteLine("2. Allocate Resources");
        Console.WriteLine("3. View Team Timesheets");
        Console.WriteLine("4. AI Features (Risk Summary & Skill Match)");
        Console.WriteLine("0. Back to Main Menu");

        var choice = InputHelper.ReadString("Select an option");

        switch (choice)
        {
            case "1":
                ConsoleRenderer.RenderWarning("My Projects screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "2":
                ConsoleRenderer.RenderWarning("Allocate Resources screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "3":
                ConsoleRenderer.RenderWarning("Team Timesheets screen not yet implemented.");
                ConsoleRenderer.Pause();
                break;
            case "4":
                ConsoleRenderer.RenderWarning("AI Features screen not yet implemented.");
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
