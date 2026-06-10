using PRM.Domain.Enums;

namespace PRM.Application.DTOs.Project;

public record CreateProjectDto(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status,
    int ManagerId,
    int TotalStoryPoints);

public record UpdateProjectDto(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status,
    int ManagerId,
    int TotalStoryPoints);

public record ProjectSummaryDto(
    int Id,
    string Name,
    string ManagerName,
    DateOnly EndDate,
    ProjectStatus Status,
    ProjectHealthStatus HealthStatus,
    int DoneStoryPoints,
    int TotalStoryPoints);

public record ProjectDetailDto(
    int Id,
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status,
    ProjectHealthStatus HealthStatus,
    int ManagerId,
    string ManagerName,
    int TotalStoryPoints,
    List<MilestoneSummaryDto> Milestones,
    List<AllocationSummaryDto> Allocations);

public record MilestoneSummaryDto(
    int Id,
    string Title,
    DateOnly DueDate,
    int StoryPoints,
    MilestoneStatus Status);

public record AllocationSummaryDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    int ProjectId,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record AddMilestoneDto(string Title, DateOnly DueDate, int StoryPoints);

public record UpdateMilestoneStatusDto(MilestoneStatus Status);
