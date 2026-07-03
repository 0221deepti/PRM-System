using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using PRM.Application.DTOs.Ai;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
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
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiService> _logger;

    public AiService(
        IEmployeeRepository employeeRepo,
        IProjectRepository projectRepo,
        ITimesheetRepository timesheetRepo,
        IAllocationRepository allocationRepo,
        ISystemConfigRepository configRepo,
        IAiProviderFactory providerFactory,
        IConfiguration configuration,
        ILogger<AiService> logger)
    {
        _employeeRepo = employeeRepo;
        _projectRepo = projectRepo;
        _timesheetRepo = timesheetRepo;
        _allocationRepo = allocationRepo;
        _configRepo = configRepo;
        _providerFactory = providerFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SkillMatchResultDto> MatchSkillsAsync(SkillMatchRequestDto request, CancellationToken ct)
    {
        if (request == null)
            throw new DomainException("Skill match request cannot be null.");

        if (string.IsNullOrWhiteSpace(request.NaturalLanguageQuery))
            throw new DomainException("Requirement query cannot be empty.");

        if (request.NaturalLanguageQuery.Trim().Length < 5)
            throw new DomainException("Requirement query must be at least 5 characters.");

        if (request.NaturalLanguageQuery.Length > 2000)
            throw new DomainException("Requirement query exceeds the maximum allowed length of 2000 characters.");

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

        string rawResponse;
        try
        {
            rawResponse = await CallProviderWithRetryAsync(config, systemPrompt, userPrompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI service call failed in MatchSkillsAsync.");
            throw new DomainException("AI service is currently unavailable. Please try again later.");
        }

        try
        {
            return ParseSkillMatchResponse(rawResponse, eligible);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI response in MatchSkillsAsync. Raw Response: {RawResponse}", rawResponse);
            throw new DomainException("AI service is currently unavailable. Please try again later.");
        }
    }

    public async Task<RiskSummaryDto> GenerateRiskSummaryAsync(RiskSummaryRequestDto request, CancellationToken ct)
    {
        if (request == null)
            throw new DomainException("Risk summary request cannot be null.");

        if (request.ProjectId <= 0)
            throw new DomainException("Project ID must be a valid positive integer.");

        if (request.ManagerEmployeeId <= 0)
            throw new DomainException("Manager employee ID must be a valid positive integer.");

        var config = await _configRepo.GetAsync(ct);
        var project = await _projectRepo.GetWithAllocationsAsync(request.ProjectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastWeek = today.AddDays(-7);
        var activeAllocations = project.Allocations.Where(a => a.IsActive).ToList();

        var effortList = new List<EffortContext>();
        foreach (var alloc in activeAllocations)
        {
            var timesheets = await _timesheetRepo.GetByProjectAndWeekAsync(project.Id, lastWeek, ct);
            var empTimesheets = timesheets.Where(t => t.UserId == alloc.UserId);
            var actualHours = empTimesheets.Sum(t => t.TotalHoursWorked);
            var expectedHours = alloc.UtilisationPercent / 100m * 40;
            effortList.Add(new EffortContext(
                alloc.User?.FullName ?? "Unknown",
                actualHours,
                expectedHours));
        }

        var milestoneContexts = project.Milestones.Select(m => new MilestoneContext(
            m.Title, m.Status.ToString(), m.DueDate,
            m.Status == MilestoneStatus.InProgress && m.DueDate < today)).ToList();

        var allocationContexts = activeAllocations.Select(a =>
            new AllocationContext(a.User?.FullName ?? "Unknown", a.UtilisationPercent)).ToList();

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

        string summary;
        try
        {
            summary = await CallProviderWithRetryAsync(config, "You are a project health analyst.", prompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI service call failed in GenerateRiskSummaryAsync.");
            throw new DomainException("AI service is currently unavailable. Please try again later.");
        }

        return new RiskSummaryDto(summary, project.Name);
    }

    private static bool HasEnoughCapacity(Domain.Entities.User user, DateOnly from, DateOnly to, int minFreePercent)
    {
        var activeAllocations = user.Allocations
            .Where(a => a.IsActive && a.FromDate <= to && a.ToDate >= from)
            .ToList();

        var totalUsed = activeAllocations.Sum(a => a.UtilisationPercent);
        return (100 - totalUsed) >= minFreePercent;
    }

    private static string BuildCandidateContext(List<Domain.Entities.User> users)
    {
        var lines = users.Select(u =>
        {
            var skills = u.Skills.Select(s => $"{s.Skill?.Name} ({s.Proficiency})").ToList();
            var allocs = u.Allocations.Where(a => a.IsActive)
                .Sum(a => a.UtilisationPercent);
            return $"ID={u.Id}, Name={u.FullName}, Department={u.Department}, " +
                   $"FreeCapacity={100 - allocs}%, Skills=[{string.Join(", ", skills)}]";
        });
        return string.Join("\n", lines);
    }

    private static SkillMatchResultDto ParseSkillMatchResponse(string rawResponse, List<Domain.Entities.User> eligible)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            throw new DomainException("The AI suggestion service returned an empty response.");

        // Try to find JSON array in response
        var start = rawResponse.IndexOf('[');
        var end = rawResponse.LastIndexOf(']');
        if (start < 0 || end < 0)
            throw new DomainException("The AI suggestion service response was not in the expected JSON array format.");

        var jsonPart = rawResponse[start..(end + 1)];
        using var doc = JsonDocument.Parse(jsonPart);
        var candidates = new List<SkillMatchCandidateDto>();

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("employeeId", out var idProp) || !item.TryGetProperty("reason", out var reasonProp))
                throw new DomainException("AI suggestion candidate is missing required fields.");

            var empId = idProp.GetInt32();
            var reason = reasonProp.GetString() ?? "";
            var emp = eligible.FirstOrDefault(e => e.Id == empId);
            if (emp != null)
            {
                var freePercent = 100 - emp.Allocations.Where(a => a.IsActive).Sum(a => a.UtilisationPercent);
                var skills = emp.Skills.Select(s => s.Skill?.Name ?? "").ToList();
                candidates.Add(new SkillMatchCandidateDto(empId, emp.FullName ?? "", reason, freePercent, skills));
            }
        }

        return new SkillMatchResultDto(candidates);
    }

    public async Task<TeamBuilderResultDto> BuildTeamAsync(TeamBuilderRequestDto request, CancellationToken ct)
    {
        if (request == null)
            throw new DomainException("Team builder request cannot be null.");

        if (string.IsNullOrWhiteSpace(request.NaturalLanguageRequirement))
            throw new DomainException("Natural language requirement cannot be empty.");

        if (request.NaturalLanguageRequirement.Trim().Length < 5)
            throw new DomainException("Natural language requirement must be at least 5 characters.");

        if (request.NaturalLanguageRequirement.Length > 2000)
            throw new DomainException("Natural language requirement query exceeds the maximum allowed length of 2000 characters.");

        var config = await _configRepo.GetAsync(ct);
        var allEmployees = await _employeeRepo.GetAllWithDetailsAsync(ct);

        // Filter: only active users in 'Employee' role are resource candidates
        var candidates = allEmployees
            .Where(e => e.IsActive && e.Role != null && e.Role.Name == "Employee")
            .ToList();

        if (candidates.Count == 0)
        {
            return new TeamBuilderResultDto(new List<TeamBuilderCandidateDto>(), "No active employee candidates available in the database.");
        }

        var context = BuildTeamBuilderResourceContext(candidates);
        var systemPrompt = """
            You are an AI-Assisted Team Builder for a Project Resource Management (PRM) system.
            Your task is to analyze the manager's project resource requirements, match them against the company resource database, and select and rank the best candidates.

            CRITICAL SELECTION LOGIC (Follow in strict order):
            1. Parse the Natural Language Requirement to identify:
               - Required skills and roles.
               - Quantity/number of resources needed for each skill/role.
               - Project duration (if specified).
               - Priority (if specified).
            2. Search the Resource Database to match candidates based on the required skills. Perform semantic mapping if needed (e.g. "React Frontend Developer" matches "React" skill; "QA Tester" matches "QA" or "Testing").
            3. Order and select matching resources using these priorities:
               - Priority 1 — Bench Resources: First select resources currently on Bench (Status = Bench, Utilisation = 0%). Bench resources are preferred because they are immediately available. Match based on skills and proficiency.
               - Priority 2 — Low Utilisation Resources: If bench resources are insufficient, select resources with available capacity (Utilisation < 100%). Prefer resources with lower utilisation percentages (e.g., 20% utilisation ranks higher than 70% utilisation).
               - Priority 3 — Best Skill Match: If resources are still needed, rank remaining resources by match score based on skill, proficiency level (Expert/Advanced, Intermediate, Beginner), relevant experience, and current utilisation.
            4. AI Matching Rule: Bench resources must always be prioritized over allocated resources.
            5. Final allocation decision remains with the Manager. Do not generate or invent new employees; only select from the provided Resource Database.

            Format the final recommendations, explainable reasons, and insights into EXACTLY the following JSON format (no markdown, no ```json formatting wrappers, just raw JSON text):
            {
              "recommendations": [
                {
                  "employeeId": 101,
                  "employeeName": "John Smith",
                  "department": "Engineering",
                  "skills": "React (Advanced), TypeScript (Intermediate)",
                  "currentUtilisation": 0,
                  "currentStatus": "Bench",
                  "matchScore": 95,
                  "recommendationReason": "Excellent match with advanced React skills, currently available on Bench (0% utilization)."
                }
              ],
              "additionalInsights": "Summary of matching (e.g., '4 of 5 requested resources were found on bench. 1 resource was selected due to limited bench availability.' or 'Only one QA resource is available. Consider hiring...').",
              "futureExtensibilityNotes": "Brief high-level summary notes that support future capabilities such as team optimization, resource shortage predictions, AI allocation suggestions, or composition risk analysis."
            }
            """;

        var userPrompt = $"Manager's Requirement:\n{request.NaturalLanguageRequirement}\n\nResource Database:\n{context}";

        string rawResponse;
        try
        {
            rawResponse = await CallProviderWithRetryAsync(config, systemPrompt, userPrompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI service call failed in BuildTeamAsync.");
            throw new DomainException("AI service is currently unavailable. Please try again later.");
        }

        try
        {
            return ParseTeamBuilderResponse(rawResponse, candidates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI response in BuildTeamAsync. Raw Response: {RawResponse}", rawResponse);
            throw new DomainException("AI service is currently unavailable. Please try again later.");
        }
    }

    private static string BuildTeamBuilderResourceContext(List<Domain.Entities.User> users)
    {
        var lines = users.Select(u =>
        {
            var skills = u.Skills.Select(s => $"{s.Skill?.Name} ({s.Proficiency})").ToList();
            var activeAllocations = u.Allocations.Where(a => a.IsActive).ToList();
            var totalUtilisation = activeAllocations.Sum(a => a.UtilisationPercent);
            
            var allocationDetails = string.Join(", ", activeAllocations.Select(a => $"{a.Project?.Name ?? "Project"}: {a.UtilisationPercent}%"));
            var allocStr = activeAllocations.Count > 0 ? $"Active allocations: [{allocationDetails}]" : "No active allocations";

            return $"EmployeeId={u.Id}, Name={u.FullName}, Department={u.Department}, Status={u.Status}, Utilisation={totalUtilisation}%, Skills=[{string.Join(", ", skills)}], {allocStr}";
        });
        return string.Join("\n", lines);
    }

    private static TeamBuilderResultDto ParseTeamBuilderResponse(string rawResponse, List<Domain.Entities.User> candidates)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            throw new DomainException("The AI team builder returned an empty response.");

        if (rawResponse.StartsWith("[AI"))
            throw new DomainException($"AI provider error: {rawResponse}");

        var start = rawResponse.IndexOf('{');
        var end = rawResponse.LastIndexOf('}');
        if (start < 0 || end < 0)
            throw new DomainException("The AI response was not in the expected JSON object format.");

        var jsonPart = rawResponse[start..(end + 1)];
        using var doc = JsonDocument.Parse(jsonPart);
        var root = doc.RootElement;

        // Verify required fields exist
        if (!root.TryGetProperty("recommendations", out var recommendationsProp) || recommendationsProp.ValueKind != JsonValueKind.Array)
            throw new DomainException("AI response is missing the required 'recommendations' array.");
        
        if (!root.TryGetProperty("additionalInsights", out _))
            throw new DomainException("AI response is missing the required 'additionalInsights' field.");

        var recommendationsList = new List<TeamBuilderCandidateDto>();
        foreach (var item in recommendationsProp.EnumerateArray())
        {
            int empId = 0;
            if (item.TryGetProperty("employeeId", out var idProp))
            {
                if (idProp.ValueKind == JsonValueKind.Number) empId = idProp.GetInt32();
                else if (idProp.ValueKind == JsonValueKind.String && int.TryParse(idProp.GetString(), out var parsedId)) empId = parsedId;
            }

            if (empId == 0)
                continue;

            var empName = item.TryGetProperty("employeeName", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var dept = item.TryGetProperty("department", out var deptProp) ? deptProp.GetString() ?? "" : "";
            var skills = item.TryGetProperty("skills", out var skillsProp) ? skillsProp.GetString() ?? "" : "";
            
            int currentUtilisation = 0;
            if (item.TryGetProperty("currentUtilisation", out var utilProp))
            {
                if (utilProp.ValueKind == JsonValueKind.Number) currentUtilisation = utilProp.GetInt32();
                else if (utilProp.ValueKind == JsonValueKind.String && int.TryParse(utilProp.GetString(), out var parsedUtil)) currentUtilisation = parsedUtil;
            }

            var currentStatus = item.TryGetProperty("currentStatus", out var statusProp) ? statusProp.GetString() ?? "" : "";
            
            int matchScore = 0;
            if (item.TryGetProperty("matchScore", out var scoreProp))
            {
                if (scoreProp.ValueKind == JsonValueKind.Number) matchScore = scoreProp.GetInt32();
                else if (scoreProp.ValueKind == JsonValueKind.String && int.TryParse(scoreProp.GetString(), out var parsedScore)) matchScore = parsedScore;
            }

            var reason = item.TryGetProperty("recommendationReason", out var reasonProp) ? reasonProp.GetString() ?? "" : "";

            var emp = candidates.FirstOrDefault(e => e.Id == empId);
            if (emp != null)
            {
                var dbSkills = string.Join(", ", emp.Skills.Select(s => $"{s.Skill?.Name} ({s.Proficiency})"));
                var dbUtil = emp.Allocations.Where(a => a.IsActive).Sum(a => a.UtilisationPercent);
                var dbStatus = emp.Status.ToString();

                recommendationsList.Add(new TeamBuilderCandidateDto(
                    empId,
                    emp.FullName ?? empName,
                    emp.Department ?? dept,
                    dbSkills,
                    dbUtil,
                    dbStatus,
                    matchScore,
                    reason));
            }
        }

        var insights = root.TryGetProperty("additionalInsights", out var insightsProp) ? insightsProp.GetString() ?? "" : "";
        var extensibility = root.TryGetProperty("futureExtensibilityNotes", out var extProp) ? extProp.GetString() ?? null : null;

        return new TeamBuilderResultDto(recommendationsList, insights, extensibility);
    }

    private async Task<string> CallProviderWithRetryAsync(
        SystemConfig config, 
        string systemPrompt, 
        string userPrompt, 
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
            throw new DomainException("Prompt cannot be empty.");

        int timeoutSeconds = 15;
        int retryCount = 3;

        try
        {
            var timeoutSection = _configuration?.GetSection("AiSettings:TimeoutSeconds");
            if (timeoutSection?.Value != null && int.TryParse(timeoutSection.Value, out var sec))
            {
                timeoutSeconds = sec;
            }

            var retrySection = _configuration?.GetSection("AiSettings:RetryCount");
            if (retrySection?.Value != null && int.TryParse(retrySection.Value, out var retries))
            {
                retryCount = retries;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read timeout/retry settings from configuration, using defaults.");
        }

        Exception? lastException = null;

        for (int attempt = 1; attempt <= retryCount; attempt++)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            ILlmProvider? provider = null;
            try
            {
                // Re-creating provider on each attempt allows factory to fall back if status changes
                provider = _providerFactory.Create(config);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create LLM provider on attempt {Attempt}", attempt);
                lastException = ex;
                continue;
            }

            var providerName = provider.GetType().Name;
            _logger.LogInformation("Sending request to provider {Provider} (Attempt {Attempt}/{MaxAttempts})", 
                providerName, attempt, retryCount);

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var response = await provider.CompleteAsync(systemPrompt, userPrompt, cts.Token);
                stopwatch.Stop();

                _logger.LogInformation("AI request succeeded. Provider: {Provider}, Response Time: {ResponseTimeMs}ms, Prompt Length: {PromptLength}",
                    providerName, stopwatch.ElapsedMilliseconds, userPrompt.Length);

                if (string.IsNullOrWhiteSpace(response))
                    throw new DomainException("The AI provider returned an empty response.");

                return response;
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning("AI request timed out on attempt {Attempt} with provider {Provider}. Elapsed: {ElapsedMs}ms",
                    attempt, providerName, stopwatch.ElapsedMilliseconds);
                lastException = new TimeoutException($"AI request timed out after {timeoutSeconds} seconds.", ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "AI request failed on attempt {Attempt} with provider {Provider}. Elapsed: {ElapsedMs}ms",
                    attempt, providerName, stopwatch.ElapsedMilliseconds);
                lastException = ex;
            }
        }

        throw new DomainException("AI service is currently unavailable. Please try again later.");
    }
}
