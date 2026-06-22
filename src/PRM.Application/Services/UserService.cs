using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;

    public UserService(
        IUserRepository users,
        IAllocationRepository allocations,
        IRepository<Role> roles,
        IRepository<Skill> skills,
        IRepository<UserSkill> userSkills,
        IConfiguration config,
        IEmailService emailService)
    {
        _users = users;
        _allocations = allocations;
        _roles = roles;
        _skills = skills;
        _userSkills = userSkills;
        _config = config;
        _emailService = emailService;
    }

    public async Task<(UserSummaryDto User, string? WarningMessage)> CreateUserAsync(CreateUserDto dto, CancellationToken ct)
    {
        ValidatePasswordStrength(dto.TemporaryPassword);

        var trimmedUsername = dto.Username.Trim();
        var trimmedEmail = dto.Email.Trim();
        var trimmedFullName = dto.FullName.Trim();
        var trimmedDept = dto.Department?.Trim() ?? "Unassigned";

        // Validate email domain config
        var allowedDomainsList = _config.GetSection("AllowedEmailDomains").GetChildren().Select(c => c.Value).Where(v => v != null).ToArray();
        var allowedDomains = allowedDomainsList.Length > 0
            ? allowedDomainsList.Cast<string>().ToArray()
            : new[] { "gmail.com", "example.com", "prm.local" };
        var emailParts = trimmedEmail.Split('@');
        if (emailParts.Length == 2)
        {
            var domain = emailParts[1].Trim().ToLowerInvariant();
            if (!allowedDomains.Any(d => d.Trim().ToLowerInvariant() == domain || d.Trim().ToLowerInvariant() == $"@{domain}"))
            {
                throw new DomainException($"Email domain '@{domain}' is not allowed.");
            }
        }
        else
        {
            throw new DomainException("Invalid email format.");
        }

        // Distinct uniqueness checks
        var existingUser = await _users.GetByUsernameAsync(trimmedUsername, ct);
        if (existingUser != null)
            throw new DomainException($"Username '{trimmedUsername}' is already taken.");

        var existingEmail = await _users.GetByEmailAsync(trimmedEmail, ct);
        if (existingEmail != null)
            throw new DomainException($"Email '{trimmedEmail}' is already registered.");

        var role = await _roles.GetByIdAsync(dto.RoleId, ct)
                   ?? throw new EntityNotFoundException("Role not found.");

        var user = new User
        {
            FullName = trimmedFullName,
            Email = trimmedEmail,
            Username = trimmedUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.TemporaryPassword),
            RoleId = dto.RoleId,
            Department = trimmedDept,
            Status = EmployeeStatus.Bench,
            IsActive = true,
            ForcePasswordChange = true
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        string? warningMessage = null;
        try
        {
            var placeholders = new Dictionary<string, string>
            {
                ["EmployeeName"] = user.FullName,
                ["Username"] = user.Username,
                ["Email"] = user.Email,
                ["TemporaryPassword"] = dto.TemporaryPassword
            };
            var emailResult = await _emailService.SendTemplateEmailAsync("Welcome New User", user.Email, placeholders, ct);
            if (!emailResult.IsSuccess)
            {
                warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
            }
        }
        catch (Exception)
        {
            warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
        }

        return (MapToDto(user, role), warningMessage);
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
