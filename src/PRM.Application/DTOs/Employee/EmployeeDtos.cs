using PRM.Domain.Enums;

namespace PRM.Application.DTOs.Employee;

public record EmployeeSummaryDto(
    int Id,
    int UserId,
    string FullName,
    string Department,
    EmployeeStatus Status,
    bool IsActive);

public record EmployeeDetailDto(
    int Id,
    int UserId,
    string FullName,
    string Department,
    EmployeeStatus Status,
    int? ManagerId,
    string? ManagerName,
    List<EmployeeSkillDto> Skills,
    List<EmployeeAllocationDto> ActiveAllocations,
    List<string> RecentActivityTags);

public record EmployeeSkillDto(
    int SkillId,
    string SkillName,
    SkillCategory Category,
    SkillProficiency Proficiency);

public record EmployeeAllocationDto(
    int AllocationId,
    int ProjectId,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record UpdateEmployeeDto(string Department);

public record AddSkillDto(string SkillName, SkillCategory Category, SkillProficiency Proficiency);

public record UpdateSkillDto(SkillProficiency Proficiency);

public record AssignManagerDto(int EmployeeUserId, int ManagerUserId);
