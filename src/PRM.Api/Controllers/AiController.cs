using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Ai;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Roles = "Manager")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService) => _aiService = aiService;

    [HttpPost("skill-match")]
    public async Task<IActionResult> SkillMatch([FromBody] SkillMatchRequestDto dto, CancellationToken ct)
    {
        var result = await _aiService.MatchSkillsAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("risk-summary")]
    public async Task<IActionResult> RiskSummary([FromBody] RiskSummaryRequestDto dto, CancellationToken ct)
    {
        var result = await _aiService.GenerateRiskSummaryAsync(dto, ct);
        return Ok(result);
    }
}
