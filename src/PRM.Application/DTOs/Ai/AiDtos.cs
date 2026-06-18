namespace PRM.Application.DTOs.Ai;

public record SkillMatchRequestDto(
    int ManagerEmployeeId,
    int ProjectId,
    string NaturalLanguageQuery,
    DateOnly FromDate,
    DateOnly ToDate,
    int MinFreePercent = 25);

public record SkillMatchCandidateDto(
    int EmployeeId,
    string EmployeeName,
    string Reason,
    int FreePercent,
    List<string> MatchingSkills);

public record SkillMatchResultDto(List<SkillMatchCandidateDto> Candidates);

public record RiskSummaryRequestDto(int ProjectId, int ManagerEmployeeId);

public record RiskSummaryDto(string Summary, string ProjectName);

public record ProjectRiskContextDto(
    string ProjectName,
    DateOnly EndDate,
    List<MilestoneContext> Milestones,
    List<AllocationContext> Allocations,
    List<EffortContext> RecentEffort);

public record MilestoneContext(string Title, string Status, DateOnly DueDate, bool IsOverdue);
public record AllocationContext(string EmployeeName, int Percent);
public record EffortContext(string EmployeeName, decimal ActualHours, decimal ExpectedHours);
