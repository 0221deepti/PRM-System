using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;
    private readonly IAllocationRepository _allocations;

    private readonly IEmailService _emailService;

    public EmployeeService(IEmployeeRepository employees, IUserRepository users, IAllocationRepository allocations, IEmailService emailService)
    {
        _employees = employees;
        _users = users;
        _allocations = allocations;
        _emailService = emailService;
    }

    public async Task<IEnumerable<EmployeeSummaryDto>> GetAllEmployeesAsync(CancellationToken ct)
    {
        var employees = await _employees.GetAllWithDetailsAsync(ct);
        return employees.Select(MapToSummary);
    }

    public async Task<IEnumerable<EmployeeSummaryDto>> GetTeamEmployeesAsync(int managerEmployeeId, CancellationToken ct)
    {
        var employees = await _employees.GetByManagerIdAsync(managerEmployeeId, ct);
        return employees.Select(MapToSummary);
    }

    public async Task<EmployeeDetailDto?> GetEmployeeDetailAsync(int employeeId, int callerUserId, string callerRole, CancellationToken ct)
    {
        var employee = await _employees.GetWithAllocationsAsync(employeeId, ct);
        if (employee == null) return null;

        if (callerRole == "Manager" && employee.ManagerId != callerUserId)
            throw new DomainException("This employee is not assigned to your team.");

        if (callerRole == "Employee" && employee.Id != callerUserId)
            throw new PrmUnauthorizedException("You can only view your own profile.");

        return await BuildDetailDto(employee, ct);
    }

    public async Task<EmployeeDetailDto?> GetEmployeeByUserIdAsync(int userId, CancellationToken ct)
    {
        var employee = await _employees.GetByUserIdAsync(userId, ct);
        if (employee == null) return null;

        var full = await _employees.GetWithAllocationsAsync(employee.Id, ct);
        if (full == null) return null;

        return await BuildDetailDto(full, ct);
    }

    public async Task UpdateEmployeeAsync(int employeeId, UpdateEmployeeDto dto, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(employeeId, ct)
                       ?? throw new EntityNotFoundException("Employee not found.");

        employee.Department = dto.Department;
        _employees.Update(employee);
        await _employees.SaveChangesAsync(ct);
    }

    public async Task DeactivateEmployeeAsync(int employeeId, CancellationToken ct)
    {
        var employee = await _employees.GetWithAllocationsAsync(employeeId, ct)
                       ?? throw new EntityNotFoundException("Employee not found.");

        // End all active allocations
        var activeAllocations = employee.Allocations.Where(a => a.IsActive).ToList();
        foreach (var allocation in activeAllocations)
        {
            allocation.IsActive = false;
            allocation.ToDate = DateOnly.FromDateTime(DateTime.UtcNow);
            _allocations.Update(allocation);
        }

        // Deactivate user account
        var user = await _users.GetByIdAsync(employee.Id, ct);
        if (user != null)
        {
            user.IsActive = false;
            _users.Update(user);
        }

        employee.Status = EmployeeStatus.Bench;
        _employees.Update(employee);
        await _employees.SaveChangesAsync(ct);
    }

    public async Task<string?> AssignManagerAsync(int employeeUserId, int managerUserId, CancellationToken ct)
    {
        var employee = await _employees.GetByUserIdAsync(employeeUserId, ct)
                       ?? throw new EntityNotFoundException("Employee not found.");

        var manager = await _employees.GetByUserIdAsync(managerUserId, ct)
                      ?? throw new EntityNotFoundException("Manager employee not found.");

        if (manager.Role?.Name != "Manager" && manager.RoleId != 2)
            throw new DomainException("The selected manager must have the Manager role.");

        if (employee.Id == manager.Id)
            throw new DomainException("An employee cannot report to themselves.");

        employee.ManagerId = manager.Id;
        _employees.Update(employee);
        await _employees.SaveChangesAsync(ct);

        string? warningMessage = null;
        try
        {
            var placeholders = new Dictionary<string, string>
            {
                ["EmployeeName"] = employee.FullName,
                ["ManagerName"] = manager.FullName,
                ["ManagerEmail"] = manager.Email
            };
            var emailResult = await _emailService.SendTemplateEmailAsync("Manager Assignment Notification", employee.Email, placeholders, ct);
            if (!emailResult.IsSuccess)
            {
                warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
            }
        }
        catch (Exception)
        {
            warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
        }
        return warningMessage;
    }

    private async Task<EmployeeDetailDto> BuildDetailDto(User employee, CancellationToken ct)
    {
        User? manager = null;
        if (employee.ManagerId.HasValue)
            manager = await _employees.GetWithSkillsAsync(employee.ManagerId.Value, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeAllocations = employee.Allocations
            .Where(a => a.IsActive && a.FromDate <= today && a.ToDate >= today)
            .Select(a => new EmployeeAllocationDto(
                a.Id, a.ProjectId, a.Project?.Name ?? "Unknown",
                a.UtilisationPercent, a.FromDate, a.ToDate))
            .ToList();

        var skills = employee.Skills
            .Select(s => new EmployeeSkillDto(s.SkillId, s.Skill?.Name ?? "", s.Skill?.Category ?? SkillCategory.Other, s.Proficiency))
            .ToList();

        return new EmployeeDetailDto(
            employee.Id, employee.Id,
            employee.FullName ?? "",
            employee.Department, employee.Status,
            employee.ManagerId,
            manager?.FullName,
            skills, activeAllocations,
            new List<string>());
    }

    private static EmployeeSummaryDto MapToSummary(User e) =>
        new(e.Id, e.Id, e.FullName ?? "", e.Department, e.Status, e.IsActive);
}

public class SkillService : ISkillService
{
    private readonly IEmployeeRepository _employees;
    private readonly IRepository<Skill> _skills;
    private readonly IRepository<UserSkill> _userSkills;

    public SkillService(
        IEmployeeRepository employees,
        IRepository<Skill> skills,
        IRepository<UserSkill> userSkills)
    {
        _employees = employees;
        _skills = skills;
        _userSkills = userSkills;
    }

    public async Task AddSkillAsync(int employeeId, AddSkillDto dto, CancellationToken ct)
    {
        var employee = await _employees.GetWithSkillsAsync(employeeId, ct)
                       ?? throw new EntityNotFoundException("Employee not found.");

        var trimmedName = dto.SkillName.Trim();

        // Find or create the skill in global database
        var allSkills = await _skills.GetAllAsync(ct);
        var globalSkill = allSkills.FirstOrDefault(s => s.Name.Trim().Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
        if (globalSkill == null)
        {
            globalSkill = new Skill
            {
                Name = trimmedName,
                Category = dto.Category
            };
            await _skills.AddAsync(globalSkill, ct);
            await _skills.SaveChangesAsync(ct);
        }

        // Check if the user is already assigned this skill
        var existingSkill = employee.Skills.FirstOrDefault(s => s.SkillId == globalSkill.Id);
        if (existingSkill != null)
            throw new DomainException($"Skill '{trimmedName}' already assigned to this employee.");

        var userSkill = new UserSkill
        {
            UserId = employeeId,
            SkillId = globalSkill.Id,
            Proficiency = dto.Proficiency
        };

        await _userSkills.AddAsync(userSkill, ct);
        await _userSkills.SaveChangesAsync(ct);
    }

    public async Task UpdateSkillProficiencyAsync(int employeeId, int skillId, UpdateSkillDto dto, CancellationToken ct)
    {
        var employee = await _employees.GetWithSkillsAsync(employeeId, ct)
                       ?? throw new EntityNotFoundException("Employee not found.");

        var empSkill = employee.Skills.FirstOrDefault(s => s.SkillId == skillId)
                       ?? throw new EntityNotFoundException("Skill not found for this employee.");

        empSkill.Proficiency = dto.Proficiency;
        _employees.Update(employee);
        await _employees.SaveChangesAsync(ct);
    }

    public async Task RemoveSkillAsync(int employeeId, int skillId, CancellationToken ct)
    {
        var employee = await _employees.GetWithSkillsAsync(employeeId, ct)
                       ?? throw new EntityNotFoundException("Employee not found.");

        var empSkill = employee.Skills.FirstOrDefault(s => s.SkillId == skillId)
                       ?? throw new EntityNotFoundException("Skill not found for this employee.");

        employee.Skills.Remove(empSkill);
        _employees.Update(employee);
        await _employees.SaveChangesAsync(ct);
    }
}
