using PRM.Client.UI;
using PRM.Application.DTOs.Ai;

namespace PRM.Client.UI.Screens;

public class AiFeaturesScreen : Screen
{
    public AiFeaturesScreen(AppServices services) : base(services) { }

    public override async Task<bool> RenderAsync()
    {
        ShowHeader("AI Features");
        Console.WriteLine("1. Generate Risk Summary");
        Console.WriteLine("2. Skill Match");
        Console.WriteLine("0. Back");

        var choice = InputHelper.ReadString("Select an option");
        try
        {
            switch (choice)
            {
                case "1":
                    var projectId = InputHelper.ReadInt("Project Id");
                    var managerId = InputHelper.ReadInt("Manager Employee Id");
                    var req = new RiskSummaryRequestDto(projectId, managerId);
                    var summary = await _services.Ai.GenerateRiskSummaryAsync(req);
                    Console.WriteLine($"\nProject: {summary.ProjectName}");
                    Console.WriteLine($"Risk Summary:\n{summary.Summary}");
                    ConsoleRenderer.Pause();
                    break;
                case "2":
                    var mgrId = InputHelper.ReadInt("Manager Employee Id");
                    var projId = InputHelper.ReadInt("Project Id");
                    var query = InputHelper.ReadString("Natural Language Query (e.g., 'frontend skills')");
                    var fromDate = InputHelper.ReadDate("From Date");
                    var toDate = InputHelper.ReadDate("To Date");
                    var matchReq = new SkillMatchRequestDto(mgrId, projId, query, fromDate, toDate, 25);
                    var res = await _services.Ai.MatchSkillsAsync(matchReq);
                    Console.WriteLine("Recommended Candidates:");
                    foreach(var r in res.Candidates)
                        Console.WriteLine($" - {r.EmployeeName} ({r.FreePercent}% free, Skills: {string.Join(", ", r.MatchingSkills)})");
                    ConsoleRenderer.Pause();
                    break;
                case "0":
                    return false;
                default:
                    ConsoleRenderer.RenderError("Invalid option.");
                    ConsoleRenderer.Pause();
                    break;
            }
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError(ex.Message);
            ConsoleRenderer.Pause();
        }

        return true;
    }
}
