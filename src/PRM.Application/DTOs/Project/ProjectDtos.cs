using System.ComponentModel.DataAnnotations;
using PRM.Domain.Enums;

namespace PRM.Application.DTOs.Project;

public record CreateProjectDto(
    [Required(ErrorMessage = "Project name is required.")] 
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Project name must be between 2 and 200 characters.")] 
    string Name,

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")] 
    string Description,

    [Required(ErrorMessage = "Start date is required.")] 
    DateOnly StartDate,

    [Required(ErrorMessage = "End date is required.")] 
    DateOnly EndDate,

    [Required(ErrorMessage = "Status is required.")] 
    ProjectStatus Status,

    [Required(ErrorMessage = "Manager ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Manager ID must be a valid positive integer.")] 
    int ManagerId,

    [Required(ErrorMessage = "Total story points is required.")] 
    [Range(0, int.MaxValue, ErrorMessage = "Total story points must be 0 or greater.")] 
    int TotalStoryPoints);

public record UpdateProjectDto(
    [Required(ErrorMessage = "Project name is required.")] 
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Project name must be between 2 and 200 characters.")] 
    string Name,

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")] 
    string Description,

    [Required(ErrorMessage = "Start date is required.")] 
    DateOnly StartDate,

    [Required(ErrorMessage = "End date is required.")] 
    DateOnly EndDate,

    [Required(ErrorMessage = "Status is required.")] 
    ProjectStatus Status,

    [Required(ErrorMessage = "Manager ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Manager ID must be a valid positive integer.")] 
    int ManagerId,

    [Required(ErrorMessage = "Total story points is required.")] 
    [Range(0, int.MaxValue, ErrorMessage = "Total story points must be 0 or greater.")] 
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
    List<ProjectAllocationSummaryDto> Allocations);

public record MilestoneSummaryDto(
    int Id,
    string Title,
    DateOnly DueDate,
    int StoryPoints,
    MilestoneStatus Status);

public record ProjectAllocationSummaryDto(
    int Id,
    int UserId,
    string UserName,
    int ProjectId,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record AddMilestoneDto(
    [Required(ErrorMessage = "Milestone title is required.")] 
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Milestone title must be between 2 and 200 characters.")] 
    string Title,

    [Required(ErrorMessage = "Milestone due date is required.")] 
    DateOnly DueDate,

    [Required(ErrorMessage = "Story points is required.")] 
    [Range(0, int.MaxValue, ErrorMessage = "Story points must be 0 or greater.")] 
    int StoryPoints);

public record UpdateMilestoneStatusDto(
    [Required(ErrorMessage = "Milestone status is required.")] 
    MilestoneStatus Status);
