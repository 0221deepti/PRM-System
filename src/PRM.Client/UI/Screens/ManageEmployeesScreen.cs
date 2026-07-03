using PRM.Client.UI;
using PRM.Application.DTOs.Employee;
using PRM.Domain.Enums;

namespace PRM.Client.UI.Screens;

public class ManageEmployeesScreen : Screen
{
    public ManageEmployeesScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Manage Employees");

        var emps = await _services.Employees.GetAllAsync();
        Console.WriteLine("Employees:");
        foreach (var e in emps)
        {
            Console.WriteLine($"{e.Id}. {e.FullName} - {e.Department} - {e.Status} - {(e.IsActive ? "Active" : "Inactive")}");
        }

        Console.WriteLine();
        Console.WriteLine("1. View Employee Detail");
        Console.WriteLine("2. Add Skill to Employee");
        Console.WriteLine("3. Deactivate Employee");
        Console.WriteLine("4. Assign Manager to Employee");
        Console.WriteLine("0. Back");

        var choice = InputHelper.ReadMenuOption("Select an option", new[] { "1", "2", "3", "4", "0" });
        try
        {
            switch (choice)
            {
                case "1":
                    var id = InputHelper.ReadInt("Employee Id");
                    var detail = await _services.Employees.GetDetailAsync(id);
                    RenderEmployeeDetail(detail);
                    ConsoleRenderer.Pause();
                    break;
                case "2":
                    await AddSkillAsync();
                    break;
                case "3":
                    {
                        var idd = InputHelper.ReadInt("Employee Id to deactivate");
                        await _services.Employees.DeactivateAsync(idd);
                        ConsoleRenderer.RenderSuccess("Employee deactivated.");
                        ConsoleRenderer.Pause();
                    }
                    break;
                case "4":
                    await AssignManagerAsync();
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

    private void RenderEmployeeDetail(EmployeeDetailDto d)
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.RenderHeader("Employee Detail");
        Console.WriteLine($"Name: {d.FullName}");
        Console.WriteLine($"Department: {d.Department}");
        Console.WriteLine($"Status: {d.Status}");
        Console.WriteLine($"Manager: {d.ManagerName ?? "-"}");
        Console.WriteLine();
        Console.WriteLine("Skills:");
        foreach (var s in d.Skills)
            Console.WriteLine($" - {s.SkillName} ({s.Proficiency})");

        Console.WriteLine();
        Console.WriteLine("Active Allocations:");
        foreach (var a in d.ActiveAllocations)
            Console.WriteLine($" - {a.ProjectName} ({a.UtilisationPercent}% from {a.FromDate} to {a.ToDate})");

        Console.WriteLine();
        Console.WriteLine("Recent Activity Tags:");
        foreach (var t in d.RecentActivityTags)
            Console.WriteLine($" - {t}");
    }

    private async Task AddSkillAsync()
    {
        var empId = InputHelper.ReadInt("Employee Id");
        var skillName = InputHelper.ReadString("Skill Name");
        Console.WriteLine("Category: 1. Backend  2. Frontend  3. DevOps  4. QA  5. Other");
        var cat = InputHelper.ReadEnumSelection<SkillCategory>("Category", 5);
        Console.WriteLine("Proficiency: 1. Beginner  2. Intermediate  3. Advanced");
        var prof = InputHelper.ReadEnumSelection<SkillProficiency>("Proficiency", 3);

        var dto = new AddSkillDto(skillName, cat, prof);
        await _services.Employees.AddSkillAsync(empId, dto);
        ConsoleRenderer.RenderSuccess("Skill added.");
        ConsoleRenderer.Pause();
    }

    private async Task AssignManagerAsync()
    {
        var empId = InputHelper.ReadInt("Employee Id");
        var managerId = InputHelper.ReadInt("Manager Id");
        var dto = new AssignManagerDto(empId, managerId);
        await _services.Employees.AssignManagerAsync(empId, dto);
        ConsoleRenderer.RenderSuccess("Manager assigned successfully.");
        ConsoleRenderer.Pause();
    }
}
