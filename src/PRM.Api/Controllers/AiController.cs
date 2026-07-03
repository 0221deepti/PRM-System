using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Ai;
using PRM.Application.DTOs.Common;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

/// <summary>
/// AI features - Skills matching and risk analysis (Manager only)
/// </summary>
[ApiController]
[Route("api/ai")]
[Authorize(Roles = "Manager")]
[Produces("application/json")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService) => _aiService = aiService;

    /// <summary>
    /// Find best-matching employees for a skill requirement (Manager only)
    /// </summary>
    /// <param name="dto">Skill name and minimum proficiency level</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">List of matching employees ranked by proficiency</response>
    /// <response code="400">Invalid skill request</response>
    /// <response code="403">Forbidden - Manager only</response>
    [HttpPost("skill-match")]
    public async Task<IActionResult> SkillMatch([FromBody] SkillMatchRequestDto dto, CancellationToken ct)
    {
        var result = await _aiService.MatchSkillsAsync(dto, ct);
        return Ok(new ApiResponse<SkillMatchResultDto>(true, "Skill match analysis completed successfully.", result));
    }

    /// <summary>
    /// Generate risk summary for project allocations (Manager only)
    /// </summary>
    /// <param name="dto">Project ID and analysis parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Risk analysis summary</response>
    /// <response code="400">Invalid project or request</response>
    /// <response code="403">Forbidden - Manager only</response>
    [HttpPost("risk-summary")]
    public async Task<IActionResult> RiskSummary([FromBody] RiskSummaryRequestDto dto, CancellationToken ct)
    {
        var result = await _aiService.GenerateRiskSummaryAsync(dto, ct);
        return Ok(new ApiResponse<RiskSummaryDto>(true, "Risk summary generated successfully.", result));
    }

    /// <summary>
    /// Recommend team members based on a natural language project requirement (Manager only)
    /// </summary>
    /// <param name="dto">Natural language requirement and manager ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Recommendation results containing ranked team members and insights</response>
    /// <response code="400">Invalid request</response>
    /// <response code="403">Forbidden - Manager only</response>
    [HttpPost("team-builder")]
    public async Task<IActionResult> BuildTeam([FromBody] TeamBuilderRequestDto dto, CancellationToken ct)
    {
        var result = await _aiService.BuildTeamAsync(dto, ct);
        return Ok(new ApiResponse<TeamBuilderResultDto>(true, "Team build recommendations generated successfully.", result));
    }
}
