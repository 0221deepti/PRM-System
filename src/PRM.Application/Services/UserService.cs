using PRM.Application.DTOs.Employee;
using PRM.Application.DTOs.User;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IAllocationRepository _allocations;
    private readonly IRepository<Role> _roles;
    private readonly IRepository<Skill> _skills;
    private readonly IRepository<UserSkill> _userSkills;

    public UserService(
        IUserRepository users,
        IAllocationRepository allocations,
        IRepository<Role> roles,
        IRepository<Skill> skills,
        IRepository<UserSkill> userSkills)
    {
        _users = users;
        _allocations = allocations;
        _roles = roles;
        _skills = skills;
        _userSkills = userSkills;
    }

    public async Task<UserSummaryDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct)
    {
        ValidatePasswordStrength(dto.TemporaryPassword);

        if (await _users.ExistsAsync(dto.Username, dto.Email, ct))
            throw new DomainException("A user with this username or email already exists.");

        var role = await _roles.GetByIdAsync(dto.RoleId, ct)
                   ?? throw new EntityNotFoundException("Role not found.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.TemporaryPassword),
            RoleId = dto.RoleId,
            Department = dto.Department ?? "Unassigned",
            Status = EmployeeStatus.Bench,
            IsActive = true,
            ForcePasswordChange = true
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        return MapToDto(user, role);
    }

    public async Task<IEnumerable<UserSummaryDto>> GetAllUsersAsync(CancellationToken ct)
    {
        var users = await _users.GetAllWithDetailsAsync(ct);
        return users.Select(u => MapToDto(u, u.Role));
    }

    public async Task DeactivateUserAsync(int userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        user.IsActive = false;
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task ReactivateUserAsync(int userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        user.IsActive = true;
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task<UserSummaryDto?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        return user == null ? null : MapToDto(user, user.Role);
    }

    public async Task<UserSummaryDto?> GetByIdAsync(int userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        return user == null ? null : MapToDto(user, user.Role);
    }

    // Employee-like methods (consolidated from Employee entity)
    public async Task<IEnumerable<EmployeeSummaryDto>> GetAllEmployeesAsync(CancellationToken ct)
    {
        var users = await _users.GetAllWithDetailsAsync(ct);
        return users.Select(MapToEmployeeSummary);
    }

    public async Task<IEnumerable<EmployeeSummaryDto>> GetTeamEmployeesAsync(int managerUserId, CancellationToken ct)
    {
        var users = await _users.GetByManagerIdAsync(managerUserId, ct);
        return users.Select(MapToEmployeeSummary);
    }

    public async Task<EmployeeDetailDto?> GetEmployeeDetailAsync(int userId, CancellationToken ct)
    {
        var user = await _users.GetWithAllocationsAsync(userId, ct);
        if (user == null) return null;

        return await BuildEmployeeDetailDto(user, ct);
    }

    public async Task UpdateEmployeeAsync(int userId, UpdateEmployeeDto dto, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        user.Department = dto.Department;
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task DeactivateEmployeeAsync(int userId, CancellationToken ct)
    {
        var user = await _users.GetWithAllocationsAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        // End all active allocations
        var activeAllocations = user.Allocations.Where(a => a.IsActive).ToList();
        foreach (var allocation in activeAllocations)
        {
            allocation.IsActive = false;
            allocation.ToDate = DateOnly.FromDateTime(DateTime.UtcNow);
            _allocations.Update(allocation);
        }

        // Deactivate user account
        user.IsActive = false;
        user.Status = EmployeeStatus.Bench;
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task AssignManagerAsync(int employeeUserId, int managerUserId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(employeeUserId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        var manager = await _users.GetByIdAsync(managerUserId, ct)
                      ?? throw new EntityNotFoundException("Manager not found.");

        user.ManagerId = manager.Id;
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<User>> GetBenchUsersAsync(int managerId, CancellationToken ct)
    {
        return await _users.GetBenchUsersAsync(managerId, ct);
    }

    // Skill methods
    public async Task AddSkillAsync(int userId, AddSkillDto dto, CancellationToken ct)
    {
        var user = await _users.GetWithSkillsAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        var existingSkill = user.Skills.FirstOrDefault(
            s => s.Skill != null && s.Skill.Name.Equals(dto.SkillName, StringComparison.OrdinalIgnoreCase));

        if (existingSkill != null)
            throw new DomainException($"Skill '{dto.SkillName}' already assigned to this user.");

        // Find or create skill
        var skill = (await _skills.GetAllAsync(ct))
            .FirstOrDefault(s => s.Name.Equals(dto.SkillName, StringComparison.OrdinalIgnoreCase));

        if (skill == null)
        {
            skill = new Skill
            {
                Name = dto.SkillName,
                Category = dto.Category
            };
            await _skills.AddAsync(skill, ct);
            await _skills.SaveChangesAsync(ct);
        }

        var userSkill = new UserSkill
        {
            UserId = userId,
            SkillId = skill.Id,
            Proficiency = dto.Proficiency
        };

        await _userSkills.AddAsync(userSkill, ct);
        await _userSkills.SaveChangesAsync(ct);
    }

    public async Task UpdateSkillProficiencyAsync(int userId, int skillId, UpdateSkillDto dto, CancellationToken ct)
    {
        var user = await _users.GetWithSkillsAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        var userSkill = user.Skills.FirstOrDefault(s => s.SkillId == skillId)
                        ?? throw new EntityNotFoundException("Skill not found for this user.");

        userSkill.Proficiency = dto.Proficiency;
        _userSkills.Update(userSkill);
        await _userSkills.SaveChangesAsync(ct);
    }

    public async Task RemoveSkillAsync(int userId, int skillId, CancellationToken ct)
    {
        var user = await _users.GetWithSkillsAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        var userSkill = user.Skills.FirstOrDefault(s => s.SkillId == skillId)
                        ?? throw new EntityNotFoundException("Skill not found for this user.");

        _userSkills.Remove(userSkill);
        await _userSkills.SaveChangesAsync(ct);
    }

    private async Task<EmployeeDetailDto> BuildEmployeeDetailDto(User user, CancellationToken ct)
    {
        User? manager = null;
        if (user.ManagerId.HasValue)
            manager = await _users.GetWithSkillsAsync(user.ManagerId.Value, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeAllocations = user.Allocations
            .Where(a => a.IsActive && a.FromDate <= today && a.ToDate >= today)
            .Select(a => new EmployeeAllocationDto(
                a.Id, a.ProjectId, a.Project?.Name ?? "Unknown",
                a.UtilisationPercent, a.FromDate, a.ToDate))
            .ToList();

        var skills = user.Skills
            .Select(s => new EmployeeSkillDto(s.SkillId, s.Skill?.Name ?? "", s.Skill?.Category ?? SkillCategory.Other, s.Proficiency))
            .ToList();

        return new EmployeeDetailDto(
            user.Id, user.Id,
            user.FullName,
            user.Department, user.Status,
            user.ManagerId,
            manager?.FullName,
            skills, activeAllocations,
            new List<string>());
    }

    private static EmployeeSummaryDto MapToEmployeeSummary(User u) =>
        new(u.Id, u.Id, u.FullName, u.Department, u.Status, u.IsActive);

    private static UserSummaryDto MapToDto(User user, Role role) =>
        new(user.Id, user.Username, user.FullName, user.Email, role.Name, user.IsActive);

    private static void ValidatePasswordStrength(string password)
    {
        if (password.Length < 8
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsDigit))
        {
            throw new DomainException(
                "Password must be 8+ characters with at least one uppercase letter and one digit.");
        }
    }
}
