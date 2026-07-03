using PRM.Application.DTOs.Project;

namespace PRM.Application.Interfaces.Services;

public interface IProjectService
{
    Task<ProjectSummaryDto> CreateProjectAsync(CreateProjectDto dto, CancellationToken ct);
    Task<IEnumerable<ProjectSummaryDto>> GetAllProjectsAsync(CancellationToken ct);
    Task<IEnumerable<ProjectSummaryDto>> GetManagerProjectsAsync(int managerEmployeeId, CancellationToken ct);
    Task<ProjectDetailDto?> GetProjectDetailAsync(int projectId, int callerUserId, string callerRole, CancellationToken ct);
    Task UpdateProjectAsync(int projectId, UpdateProjectDto dto, CancellationToken ct);
}

public interface IMilestoneService
{
    Task<MilestoneSummaryDto> AddMilestoneAsync(int projectId, AddMilestoneDto dto, int callerUserId, string callerRole, CancellationToken ct);
    Task UpdateMilestoneStatusAsync(int milestoneId, UpdateMilestoneStatusDto dto, int callerUserId, string callerRole, CancellationToken ct);
    Task<IEnumerable<MilestoneSummaryDto>> GetProjectMilestonesAsync(int projectId, int callerUserId, string callerRole, CancellationToken ct);
}
