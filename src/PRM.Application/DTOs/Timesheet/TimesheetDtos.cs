namespace PRM.Application.DTOs.Timesheet;

public record SubmitTimesheetDto(
    int ProjectId,
    DateOnly WeekStartDate,
    decimal HoursWorked,
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
