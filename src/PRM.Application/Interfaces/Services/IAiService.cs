using PRM.Application.DTOs.Ai;

namespace PRM.Application.Interfaces.Services;

public interface IAiService
{
    Task<SkillMatchResultDto> MatchSkillsAsync(SkillMatchRequestDto request, CancellationToken ct);
    Task<RiskSummaryDto> GenerateRiskSummaryAsync(RiskSummaryRequestDto request, CancellationToken ct);
}

public interface IHealthFlaggingService
{
    Task ComputeEmployeeStatusesAsync(CancellationToken ct);
    Task FlagProjectHealthAsync(CancellationToken ct);
}

public interface ITokenService
{
    string GenerateToken(PRM.Domain.Entities.User user, int employeeId);
}
