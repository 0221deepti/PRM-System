using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repositories;

public interface IEmployeeAccessStatusRepository : IRepository<EmployeeAccessStatus>
{
    Task<EmployeeAccessStatus?> GetByEmployeeAndWeekAsync(int employeeId, DateOnly trackedWeekStartDate, CancellationToken ct = default);
    Task<EmployeeAccessStatus?> GetLatestForEmployeeAsync(int employeeId, CancellationToken ct = default);
    Task<EmployeeAccessStatus?> GetFrozenStatusAsync(int employeeId, CancellationToken ct = default);
}