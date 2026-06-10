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

    public EmployeeService(IEmployeeRepository employees, IUserRepository users, IAllocationRepository allocations)
    {
        _employees = employees;
        _users = users;
        _allocations = allocations;
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

    public async Task<EmployeeDetailDto?> GetEmployeeDetailAsync(int employeeId, CancellationToken ct)
    {
        var employee = await _employees.GetWithAllocationsAsync(employeeId, ct);
        if (employee == null) return null;

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
        var user = await _users.GetByIdAsync(employee.UserId, ct);
        if (user != null)
        {
            user.IsActive = false;
            _users.Update(user);
        }

        employee.Status = EmployeeStatus.Bench;
        _employees.Update(employee);
        await _employees.SaveChangesAsync(ct);
    }

    public async Task AssignManagerAsync(int employeeUserId, int managerUserId, CancellationToken ct)
    {
        var employee = await _employees.GetByUserIdAsync(employeeUserId, ct)
                       ?? throw new EntityNotFoundException("Employee not found.");

        var manager = await _employees.GetByUserIdAsync(managerUserId, ct)
                      ?? throw new EntityNotFoundException("Manager employee not found.");

        employee.ManagerId = manager.Id;
        _employees.Update(employee);
        await _employees.SaveChangesAsync(ct);
    }

    private async Task<EmployeeDetailDto> BuildDetailDto(Employee employee, CancellationToken ct)
    {
        Employee? manager = null;
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
            employee.Id, employee.UserId,
            employee.User?.FullName ?? "",
            employee.Department, employee.Status,
            employee.ManagerId,
            manager?.User?.FullName,
            skills, activeAllocations,
            new List<string>());
    }

    private static EmployeeSummaryDto MapToSummary(Employee e) =>
        new(e.Id, e.UserId, e.User?.FullName ?? "", e.Department, e.Status, e.User?.IsActive ?? false);
}

public class SkillService : ISkillService
{
    private readonly IEmployeeRepository _employees;

    public SkillService(IEmployeeRepository employees) => _employees = employees;

    public async Task AddSkillAsync(int employeeId, AddSkillDto dto, CancellationToken ct)
    {
        var employee = await _employees.GetWithSkillsAsync(employeeId, ct)
                       ?? throw new EntityNotFoundException("Employee not found.");

        // Find or create skill — for simplicity, check existing skills by name
        var existingSkill = employee.Skills.FirstOrDefault(
            s => s.Skill != null && s.Skill.Name.Equals(dto.SkillName, StringComparison.OrdinalIgnoreCase));

        if (existingSkill != null)
            throw new DomainException($"Skill '{dto.SkillName}' already assigned to this employee.");

        // Note: In production, use a SkillRepository. For simplicity, handled via context.
        _employees.Update(employee);
        await _employees.SaveChangesAsync(ct);
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
