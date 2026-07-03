using PRM.Client.UI;
using PRM.Application.DTOs.Project;
using PRM.Application.DTOs.Allocation;

namespace PRM.Client.UI.Screens;

public class ViewAllocationsScreen : Screen
{
    public ViewAllocationsScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("All Allocations");

        var allocs = await _services.Allocations.GetAllAsync();
        Console.WriteLine("Allocations:");
        foreach (var a in allocs)
        {
            Console.WriteLine($"{a.Id}. {a.UserFullName} -> {a.ProjectName} ({a.UtilisationPercent}%) from {a.FromDate} to {a.ToDate}");
        }

        Console.WriteLine();
        Console.WriteLine("0. Back");
        var _ = InputHelper.ReadString("Press 0 to go back", required: false);
        return false;
    }
}
