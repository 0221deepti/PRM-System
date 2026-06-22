using System.ComponentModel.DataAnnotations;
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

public record UpdateEmployeeDto(
    [Required(ErrorMessage = "Department is required.")] 
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Department name must be between 1 and 100 characters.")] 
    string Department);

public record AddSkillDto(
    [Required(ErrorMessage = "Skill name is required.")] 
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Skill name must be between 1 and 100 characters.")] 
    string SkillName,

    [Required(ErrorMessage = "Skill category is required.")] 
    SkillCategory Category,

    [Required(ErrorMessage = "Skill proficiency is required.")] 
    SkillProficiency Proficiency);

public record UpdateSkillDto(
    [Required(ErrorMessage = "Skill proficiency is required.")] 
    SkillProficiency Proficiency);

public record AssignManagerDto(
    [Required(ErrorMessage = "Employee user ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Employee user ID must be a valid positive integer.")] 
    int EmployeeUserId,

    [Required(ErrorMessage = "Manager user ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Manager user ID must be a valid positive integer.")] 
    int ManagerUserId);
