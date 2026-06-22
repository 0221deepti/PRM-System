using PRM.Application.DTOs.Allocation;
using PRM.Application.DTOs.Project;

namespace PRM.Application.Interfaces.Services;

public interface IAllocationService
{
    Task<(AllocationSummaryDto Allocation, string? WarningMessage)> AllocateAsync(CreateAllocationDto dto, int managerEmployeeId, CancellationToken ct);
    Task EndAllocationAsync(int allocationId, int managerEmployeeId, CancellationToken ct);
    Task<IEnumerable<AllocationSummaryDto>> GetAllAllocationsAsync(CancellationToken ct);
    Task<IEnumerable<AllocationSummaryDto>> GetActiveAllocationsByProjectAsync(int projectId, int callerUserId, string callerRole, CancellationToken ct);
    Task<IEnumerable<AllocationSummaryDto>> GetMyAllocationsAsync(int employeeId, CancellationToken ct);
    Task<IEnumerable<AllocationSummaryDto>> GetActiveAllocationsForWeekAsync(int employeeId, DateOnly weekStart, CancellationToken ct);
}
