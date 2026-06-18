using PRM.Client.UI;
using PRM.Application.DTOs.Employee;

namespace PRM.Client.UI.Screens;

public class MyProfileScreen : Screen
{
    public MyProfileScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("My Profile");
        var detail = await _services.Employees.GetMyProfileAsync();
        Console.WriteLine($"Name: {detail.FullName}");
        Console.WriteLine($"Department: {detail.Department}");
        Console.WriteLine($"Status: {detail.Status}");
        Console.WriteLine("Skills:");
        foreach(var s in detail.Skills)
            Console.WriteLine($" - {s.SkillName} ({s.Proficiency})");

        Console.WriteLine();
        Console.WriteLine("0. Back");
        var _ = InputHelper.ReadString("Press 0 to go back", required: false);
        return false;
    }
}
