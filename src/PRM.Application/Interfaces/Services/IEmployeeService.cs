using PRM.Application.DTOs.Employee;

namespace PRM.Application.Interfaces.Services;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeSummaryDto>> GetAllEmployeesAsync(CancellationToken ct);
    Task<IEnumerable<EmployeeSummaryDto>> GetTeamEmployeesAsync(int managerEmployeeId, CancellationToken ct);
    Task<EmployeeDetailDto?> GetEmployeeDetailAsync(int employeeId, CancellationToken ct);
    Task UpdateEmployeeAsync(int employeeId, UpdateEmployeeDto dto, CancellationToken ct);
    Task DeactivateEmployeeAsync(int employeeId, CancellationToken ct);
    Task AssignManagerAsync(int employeeUserId, int managerUserId, CancellationToken ct);
    Task<EmployeeDetailDto?> GetEmployeeByUserIdAsync(int userId, CancellationToken ct);
}

public interface ISkillService
{
    Task AddSkillAsync(int employeeId, AddSkillDto dto, CancellationToken ct);
    Task UpdateSkillProficiencyAsync(int employeeId, int skillId, UpdateSkillDto dto, CancellationToken ct);
    Task RemoveSkillAsync(int employeeId, int skillId, CancellationToken ct);
}
