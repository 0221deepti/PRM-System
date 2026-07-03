using System.ComponentModel.DataAnnotations;

namespace PRM.Application.DTOs.Ai;

public record SkillMatchRequestDto(
    [Required(ErrorMessage = "Manager employee ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Manager employee ID must be a valid positive integer.")] 
    int ManagerEmployeeId,
    [Required(ErrorMessage = "Project ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Project ID must be a valid positive integer.")] 
    int ProjectId,
    [Required(ErrorMessage = "Query is required.")] 
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "Query must be between 5 and 2000 characters.")] 
    string NaturalLanguageQuery,
    [Required(ErrorMessage = "Start date is required.")] 
    DateOnly FromDate,
    [Required(ErrorMessage = "End date is required.")] 
    DateOnly ToDate,
    [Range(0, 100, ErrorMessage = "Minimum free percent must be between 0 and 100.")] 
    int MinFreePercent = 25);

public record SkillMatchCandidateDto(
    int EmployeeId,
    string EmployeeName,
    string Reason,
    int FreePercent,
    List<string> MatchingSkills);

public record SkillMatchResultDto(List<SkillMatchCandidateDto> Candidates);

public record RiskSummaryRequestDto(
    [Required(ErrorMessage = "Project ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Project ID must be a valid positive integer.")] 
    int ProjectId,
    [Required(ErrorMessage = "Manager employee ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Manager employee ID must be a valid positive integer.")] 
    int ManagerEmployeeId);

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

public record TeamBuilderRequestDto(
    [Required(ErrorMessage = "Manager employee ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Manager employee ID must be a valid positive integer.")] 
    int ManagerEmployeeId,
    [Required(ErrorMessage = "Requirement text is required.")] 
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "Requirement text must be between 5 and 2000 characters.")] 
    string NaturalLanguageRequirement);

public record TeamBuilderCandidateDto(
    int EmployeeId,
    string EmployeeName,
    string Department,
    string Skills,
    int CurrentUtilisation,
    string CurrentStatus,
    int MatchScore,
    string RecommendationReason);

public record TeamBuilderResultDto(
    List<TeamBuilderCandidateDto> Recommendations,
    string AdditionalInsights,
    string? FutureExtensibilityNotes = null);

