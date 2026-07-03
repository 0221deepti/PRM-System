using PRM.Client.UI;
using PRM.Application.DTOs.Employee;

namespace PRM.Client.UI.Screens;

public class MyAllocationsScreen : Screen
{
    public MyAllocationsScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("My Allocations");
        var list = await _services.Allocations.GetMineAsync();
        Console.WriteLine("My Allocations:");
        foreach(var a in list)
            Console.WriteLine($"{a.Id}. {a.ProjectName} - {a.UtilisationPercent}% from {a.FromDate} to {a.ToDate}");

        Console.WriteLine();
        Console.WriteLine("0. Back");
        var _ = InputHelper.ReadString("Press 0 to go back", required: false);
        return false;
    }
}
