using PRM.Client.UI;
using PRM.Application.DTOs.Project;
using PRM.Domain.Enums;

namespace PRM.Client.UI.Screens;

public class ManageProjectsScreen : Screen
{
    public ManageProjectsScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Manage Projects");

        var projects = await _services.Projects.GetAllAsync();
        Console.WriteLine("Projects:");
        foreach (var p in projects)
        {
            Console.WriteLine($"{p.Id}. {p.Name} - Manager: {p.ManagerName} - Status: {p.Status} - Health: {p.HealthStatus}");
        }

        Console.WriteLine();
        Console.WriteLine("1. Create Project");
        Console.WriteLine("2. View Project Details");
        Console.WriteLine("0. Back");

        var choice = InputHelper.ReadMenuOption("Select an option", new[] { "1", "2", "0" });
        try
        {
            switch (choice)
            {
                case "1":
                    await CreateProjectAsync();
                    break;
                case "2":
                    var id = InputHelper.ReadInt("Project Id");
                    var detail = await _services.Projects.GetDetailAsync(id);
                    RenderProjectDetail(detail);
                    ConsoleRenderer.Pause();
                    break;
                case "0":
                    return false;
            }
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError(ex.Message);
            ConsoleRenderer.Pause();
        }

        return true;
    }

    private async Task CreateProjectAsync()
    {
        ShowHeader("Create Project");
        var name = InputHelper.ReadString("Name");
        var desc = InputHelper.ReadString("Description");
        var start = InputHelper.ReadDate("Start Date");
        var end = InputHelper.ReadDate("End Date");
        Console.WriteLine("Status: 1. Planned  2. Active  3. OnHold  4. Completed");
        var status = InputHelper.ReadEnumSelection<ProjectStatus>("Status", 4);
        var managerId = InputHelper.ReadInt("Manager Employee Id");
        var points = InputHelper.ReadInt("Total Story Points");

        var dto = new CreateProjectDto(name, desc, start, end, status, managerId, points);
        await _services.Projects.CreateAsync(dto);
        ConsoleRenderer.RenderSuccess("Project created.");
        ConsoleRenderer.Pause();
    }

    private void RenderProjectDetail(ProjectDetailDto d)
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.RenderHeader("Project Detail");
        Console.WriteLine($"Name: {d.Name}");
        Console.WriteLine($"Description: {d.Description}");
        Console.WriteLine($"Manager: {d.ManagerName}");
        Console.WriteLine($"Status: {d.Status}");
        Console.WriteLine($"Health: {d.HealthStatus}");
        Console.WriteLine();
        Console.WriteLine("Milestones:");
        foreach (var m in d.Milestones)
            Console.WriteLine($" - ({m.Status}) {m.Title} due {m.DueDate} [{m.StoryPoints} pts]");

        Console.WriteLine();
        Console.WriteLine("Allocations:");
        foreach (var a in d.Allocations)
            Console.WriteLine($" - {a.UserName} ({a.UtilisationPercent}%) from {a.FromDate} to {a.ToDate}");
    }
}
