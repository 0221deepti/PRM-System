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

        var choice = InputHelper.ReadMenuOption("Select an option", new[] { "1", "2", "3", "4", "0" });

        switch (choice)
        {
            case "1":
                await new MyProjectsScreen(_services).RenderAsync();
                break;
            case "2":
                await new AllocateResourcesScreen(_services).RenderAsync();
                break;
            case "3":
                await new TeamTimesheetsScreen(_services).RenderAsync();
                break;
            case "4":
                await new AiFeaturesScreen(_services).RenderAsync();
                break;
            case "0":
                return false;
        }

        return true;
    }
}
