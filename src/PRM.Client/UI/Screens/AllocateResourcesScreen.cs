using PRM.Client.UI;
using PRM.Application.DTOs.Allocation;

namespace PRM.Client.UI.Screens;

public class AllocateResourcesScreen : Screen
{
    public AllocateResourcesScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("Allocate Resources");

        var empId = InputHelper.ReadInt("Employee Id");
        var projectId = InputHelper.ReadInt("Project Id");
        var util = InputHelper.ReadPercentage("Utilisation Percent");
        var from = InputHelper.ReadDate("From Date");
        var to = InputHelper.ReadDate("To Date");

        var dto = new CreateAllocationDto(empId, projectId, util, from, to);
        await _services.Allocations.CreateAsync(dto!);
        ConsoleRenderer.RenderSuccess("Allocation created.");
        ConsoleRenderer.Pause();
        return false;
    }
}
