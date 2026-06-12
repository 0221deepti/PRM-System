using PRM.Application.DTOs.Employee;
using PRM.Application.DTOs.User;
using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserSummaryDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct);
    Task<IEnumerable<UserSummaryDto>> GetAllUsersAsync(CancellationToken ct);
    Task DeactivateUserAsync(int userId, CancellationToken ct);
    Task ReactivateUserAsync(int userId, CancellationToken ct);
    Task<UserSummaryDto?> GetByUsernameAsync(string username, CancellationToken ct);
    Task<UserSummaryDto?> GetByIdAsync(int userId, CancellationToken ct);

    // Employee-like methods (consolidated from Employee entity)
    Task<IEnumerable<EmployeeSummaryDto>> GetAllEmployeesAsync(CancellationToken ct);
    Task<IEnumerable<EmployeeSummaryDto>> GetTeamEmployeesAsync(int managerUserId, CancellationToken ct);
    Task<EmployeeDetailDto?> GetEmployeeDetailAsync(int userId, CancellationToken ct);
    Task UpdateEmployeeAsync(int userId, UpdateEmployeeDto dto, CancellationToken ct);
    Task DeactivateEmployeeAsync(int userId, CancellationToken ct);
    Task AssignManagerAsync(int employeeUserId, int managerUserId, CancellationToken ct);
    Task<IEnumerable<User>> GetBenchUsersAsync(int managerId, CancellationToken ct);

    // Skill methods
    Task AddSkillAsync(int userId, AddSkillDto dto, CancellationToken ct);
    Task UpdateSkillProficiencyAsync(int userId, int skillId, UpdateSkillDto dto, CancellationToken ct);
    Task RemoveSkillAsync(int userId, int skillId, CancellationToken ct);
}
