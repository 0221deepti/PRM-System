using PRM.Client.UI;
using PRM.Application.DTOs.Project;

namespace PRM.Client.UI.Screens;

public class MyProjectsScreen : Screen
{
    public MyProjectsScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("My Projects");
        var projects = await _services.Projects.GetMineAsync();
        Console.WriteLine("My Projects:");
        foreach (var p in projects)
            Console.WriteLine($"{p.Id}. {p.Name} - {p.Status} - Health: {p.HealthStatus}");

        Console.WriteLine();
        Console.WriteLine("0. Back");
        var _ = InputHelper.ReadString("Press 0 to go back", required: false);
        return false;
    }
}
