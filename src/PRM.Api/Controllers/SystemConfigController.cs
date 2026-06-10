using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Config;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/config")]
[Authorize(Roles = "Admin")]
public class SystemConfigController : ControllerBase
{
    private readonly ISystemConfigService _configService;

    public SystemConfigController(ISystemConfigService configService) => _configService = configService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var config = await _configService.GetConfigAsync(ct);
        return Ok(config);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateConfigDto dto, CancellationToken ct)
    {
        await _configService.UpdateConfigAsync(dto, ct);
        return Ok(new { message = "Configuration updated." });
    }
}
