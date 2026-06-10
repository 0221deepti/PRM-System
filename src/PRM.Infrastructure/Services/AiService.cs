using System.Text.Json;
using PRM.Application.DTOs.Ai;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Enums;
using PRM.Infrastructure.AI;

namespace PRM.Infrastructure.Services;

public class AiService : IAiService
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ITimesheetRepository _timesheetRepo;
    private readonly IAllocationRepository _allocationRepo;
    private readonly ISystemConfigRepository _configRepo;
    private readonly IAiProviderFactory _providerFactory;

    public AiService(
        IEmployeeRepository employeeRepo,
        IProjectRepository projectRepo,
        ITimesheetRepository timesheetRepo,
        IAllocationRepository allocationRepo,
        ISystemConfigRepository configRepo,
        IAiProviderFactory providerFactory)
    {
        _employeeRepo = employeeRepo;
        _projectRepo = projectRepo;
        _timesheetRepo = timesheetRepo;
        _allocationRepo = allocationRepo;
        _configRepo = configRepo;
        _providerFactory = providerFactory;
    }

    public async Task<SkillMatchResultDto> MatchSkillsAsync(SkillMatchRequestDto request, CancellationToken ct)
    {
        var config = await _configRepo.GetAsync(ct);
        var candidates = await _employeeRepo.GetByManagerIdAsync(request.ManagerEmployeeId, ct);

        var eligible = candidates
            .Where(e => HasEnoughCapacity(e, request.FromDate, request.ToDate, request.MinFreePercent))
            .ToList();

        if (eligible.Count == 0)
            return new SkillMatchResultDto(new List<SkillMatchCandidateDto>());

        var context = BuildCandidateContext(eligible);
        var systemPrompt = """
            You are a resource allocation assistant. Given a project requirement and a list of 
            candidate employees with their skills, allocations, and recent activity tags, 
            rank the top 3 candidates and give a concise reason for each (1-2 sentences). 
            Respond ONLY in the following JSON format (no markdown, no code blocks):
            [
              { "employeeId": 101, "reason": "..." },
              ...
            ]
            Do not include anyone who is not in the provided candidate list.
            """;

        var userPrompt = $"Requirement: {request.NaturalLanguageQuery}\n\nCandidates:\n{context}";

        var provider = _providerFactory.Create(config.LlmProvider, config.LlmApiKey);
        var rawResponse = await provider.CompleteAsync(systemPrompt, userPrompt, ct);

        return ParseSkillMatchResponse(rawResponse, eligible);
    }

    public async Task<RiskSummaryDto> GenerateRiskSummaryAsync(RiskSummaryRequestDto request, CancellationToken ct)
    {
        var config = await _configRepo.GetAsync(ct);
        var project = await _projectRepo.GetWithAllocationsAsync(request.ProjectId, ct);
        if (project == null)
            return new RiskSummaryDto("Project not found.", "Unknown");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastWeek = today.AddDays(-7);
        var activeAllocations = project.Allocations.Where(a => a.IsActive).ToList();

        var effortList = new List<EffortContext>();
        foreach (var alloc in activeAllocations)
        {
            var timesheets = await _timesheetRepo.GetByProjectAndWeekAsync(project.Id, lastWeek, ct);
            var empTimesheets = timesheets.Where(t => t.EmployeeId == alloc.EmployeeId);
            var actualHours = empTimesheets.Sum(t => t.HoursWorked);
            var expectedHours = alloc.UtilisationPercent / 100m * 40;
            effortList.Add(new EffortContext(
                alloc.Employee?.User?.FullName ?? "Unknown",
                actualHours,
                expectedHours));
        }

        var milestoneContexts = project.Milestones.Select(m => new MilestoneContext(
            m.Title, m.Status.ToString(), m.DueDate,
            m.Status == MilestoneStatus.InProgress && m.DueDate < today)).ToList();

        var allocationContexts = activeAllocations.Select(a =>
            new AllocationContext(a.Employee?.User?.FullName ?? "Unknown", a.UtilisationPercent)).ToList();

        var prompt = $"""
            Project: {project.Name}
            End Date: {project.EndDate}
            Health Status: {project.HealthStatus}
            
            Milestones:
            {string.Join("\n", milestoneContexts.Select(m => $"  - {m.Title}: {m.Status}, Due {m.DueDate}{(m.IsOverdue ? " [OVERDUE]" : "")}"))}
            
            Allocated Team:
            {string.Join("\n", allocationContexts.Select(a => $"  - {a.EmployeeName} ({a.Percent}%)"))}
            
            Recent Effort (last week):
            {string.Join("\n", effortList.Select(e => $"  - {e.EmployeeName}: {e.ActualHours:F1} hrs (expected {e.ExpectedHours:F1} hrs)"))}
            
            Write a brief 3-5 sentence plain English risk summary for the delivery manager.
            Highlight specific concerns. Do not repeat raw numbers unnecessarily.
            """;

        var provider = _providerFactory.Create(config.LlmProvider, config.LlmApiKey);
        var summary = await provider.CompleteAsync("You are a project health analyst.", prompt, ct);

        return new RiskSummaryDto(summary, project.Name);
    }

    private static bool HasEnoughCapacity(Domain.Entities.Employee employee, DateOnly from, DateOnly to, int minFreePercent)
    {
        var activeAllocations = employee.Allocations
            .Where(a => a.IsActive && a.FromDate <= to && a.ToDate >= from)
            .ToList();

        var totalUsed = activeAllocations.Sum(a => a.UtilisationPercent);
        return (100 - totalUsed) >= minFreePercent;
    }

    private static string BuildCandidateContext(List<Domain.Entities.Employee> employees)
    {
        var lines = employees.Select(e =>
        {
            var skills = e.Skills.Select(s => $"{s.Skill?.Name} ({s.Proficiency})").ToList();
            var allocs = e.Allocations.Where(a => a.IsActive)
                .Sum(a => a.UtilisationPercent);
            return $"ID={e.Id}, Name={e.User?.FullName}, Department={e.Department}, " +
                   $"FreeCapacity={100 - allocs}%, Skills=[{string.Join(", ", skills)}]";
        });
        return string.Join("\n", lines);
    }

    private static SkillMatchResultDto ParseSkillMatchResponse(string rawResponse, List<Domain.Entities.Employee> eligible)
    {
        try
        {
            // Try to find JSON array in response
            var start = rawResponse.IndexOf('[');
            var end = rawResponse.LastIndexOf(']');
            if (start < 0 || end < 0)
                return new SkillMatchResultDto(new List<SkillMatchCandidateDto>());

            var jsonPart = rawResponse[start..(end + 1)];
            using var doc = JsonDocument.Parse(jsonPart);
            var candidates = new List<SkillMatchCandidateDto>();

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var empId = item.GetProperty("employeeId").GetInt32();
                var reason = item.GetProperty("reason").GetString() ?? "";
                var emp = eligible.FirstOrDefault(e => e.Id == empId);
                if (emp != null)
                {
                    var freePercent = 100 - emp.Allocations.Where(a => a.IsActive).Sum(a => a.UtilisationPercent);
                    var skills = emp.Skills.Select(s => s.Skill?.Name ?? "").ToList();
                    candidates.Add(new SkillMatchCandidateDto(empId, emp.User?.FullName ?? "", reason, freePercent, skills));
                }
            }

            return new SkillMatchResultDto(candidates);
        }
        catch
        {
            return new SkillMatchResultDto(new List<SkillMatchCandidateDto>());
        }
    }
}
