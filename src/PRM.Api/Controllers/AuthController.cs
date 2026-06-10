using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Auth;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto.Username, dto.Password, ct);
        return Ok(result);
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest(new { error = "Passwords do not match." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authService.ChangePasswordAsync(userId, dto.NewPassword, ct);
        return Ok(new { message = "Password updated successfully." });
    }

    [HttpPut("reset-password/{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(int userId, [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(userId, dto.NewPassword, ct);
        return Ok(new { message = "Password reset. User will be prompted to change it on next login." });
    }
}
