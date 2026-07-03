using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Common;
using PRM.Application.DTOs.Config;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

/// <summary>
/// System configuration - Admin only. Manage application-wide settings.
/// </summary>
[ApiController]
[Route("api/config")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class SystemConfigController : ControllerBase
{
    private readonly ISystemConfigService _configService;

    public SystemConfigController(ISystemConfigService configService) => _configService = configService;

    /// <summary>
    /// Get current system configuration (Admin only)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">System configuration</response>
    /// <response code="403">Forbidden - Admin role required</response>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var config = await _configService.GetConfigAsync(ct);
        return Ok(new ApiResponse<SystemConfigDto>(true, "System configuration retrieved successfully.", config));
    }

    /// <summary>
    /// Update system configuration (Admin only)
    /// </summary>
    /// <param name="dto">Configuration update</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Configuration updated</response>
    /// <response code="400">Invalid configuration</response>
    /// <response code="403">Forbidden - Admin role required</response>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateConfigDto dto, CancellationToken ct)
    {
        await _configService.UpdateConfigAsync(dto, ct);
        return Ok(new ApiResponse(true, "Configuration updated successfully."));
    }
}
