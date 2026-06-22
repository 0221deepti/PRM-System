using System.ComponentModel.DataAnnotations;

namespace PRM.Application.DTOs.Timesheet;

public record SubmitTimesheetDto(
    [Required(ErrorMessage = "Project ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Project ID must be a valid positive integer.")] 
    int ProjectId,

    [Required(ErrorMessage = "Week start date is required.")] 
    DateOnly WeekStartDate,

    [Required(ErrorMessage = "Hours worked is required.")] 
    [Range(0.01, 168.0, ErrorMessage = "Hours worked must be greater than 0.")] 
    decimal HoursWorked,

    [Required(ErrorMessage = "Activity tags are required.")] 
    List<string> ActivityTags);

public record TimesheetSummaryDto(
    int Id,
    int ProjectId,
    string ProjectName,
    DateOnly WeekStartDate,
    decimal HoursWorked,
    string ActivityTags,
    bool IsSubmitted);

public record TimesheetWeekSummaryDto(
    DateOnly WeekStartDate,
    decimal TotalHours,
    bool HasSubmission,
    List<TimesheetSummaryDto> Entries);

public record TeamTimesheetEntryDto(
    string EmployeeName,
    int EmployeeId,
    string ProjectName,
    int ProjectId,
    decimal HoursWorked,
    bool IsSubmitted,
    DateOnly WeekStartDate);
