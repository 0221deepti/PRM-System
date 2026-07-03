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
        Console.WriteLine("3. AI-Assisted Team Builder");
        Console.WriteLine("0. Back");

        var choice = InputHelper.ReadMenuOption("Select an option", new[] { "1", "2", "3", "0" });
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
                case "3":
                    var builderMgrId = InputHelper.ReadInt("Manager Employee Id");
                    var requirement = ReadMultilineRequirement();
                    
                    Console.WriteLine("\nGenerating team recommendations. Please wait...");
                    var teamReq = new TeamBuilderRequestDto(builderMgrId, requirement);
                    var teamRes = await _services.Ai.BuildTeamAsync(teamReq);
                    
                    Console.WriteLine("\n" + new string('=', 60));
                    Console.WriteLine("                    RECOMMENDED CANDIDATES");
                    Console.WriteLine(new string('=', 60));

                    if (teamRes.Recommendations == null || teamRes.Recommendations.Count == 0)
                    {
                        Console.WriteLine("No candidate recommendations found.");
                    }
                    else
                    {
                        int index = 1;
                        foreach (var cand in teamRes.Recommendations)
                        {
                            Console.WriteLine($"Candidate #{index}");
                            Console.WriteLine(new string('-', 30));
                            Console.WriteLine($"Employee Name:       {cand.EmployeeName}");
                            Console.WriteLine($"Department:          {cand.Department}");
                            Console.WriteLine($"Skills & Prof:       {cand.Skills}");
                            Console.WriteLine($"Current Utilisation: {cand.CurrentUtilisation}%");
                            Console.WriteLine($"Current Status:      {cand.CurrentStatus}");
                            Console.WriteLine($"Match Score:         {cand.MatchScore}%");
                            Console.WriteLine($"Recommendation Reason:\n{cand.RecommendationReason}");
                            Console.WriteLine(new string('-', 60));
                            index++;
                        }
                    }

                    Console.WriteLine("\n" + new string('=', 60));
                    Console.WriteLine("                    ADDITIONAL AI INSIGHTS");
                    Console.WriteLine(new string('=', 60));
                    Console.WriteLine(teamRes.AdditionalInsights);

                    if (!string.IsNullOrEmpty(teamRes.FutureExtensibilityNotes))
                    {
                        Console.WriteLine("\nFuture Extensibility Notes / Predictions:");
                        Console.WriteLine(teamRes.FutureExtensibilityNotes);
                    }
                    Console.WriteLine(new string('=', 60));
                    
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

    private string ReadMultilineRequirement()
    {
        Console.WriteLine("Enter Natural Language requirement (press Enter on an empty line to finish):");
        var lines = new List<string>();
        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                if (lines.Count > 0) break;
                Console.WriteLine("Requirement text cannot be empty. Please enter your project resource requirement.");
                continue;
            }
            lines.Add(line);
        }
        return string.Join(Environment.NewLine, lines);
    }
}
