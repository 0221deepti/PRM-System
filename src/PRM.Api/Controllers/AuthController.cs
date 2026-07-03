using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Auth;
using PRM.Application.DTOs.Common;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

/// <summary>
/// Authentication endpoints - login, change password, reset password
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>
    /// Authenticate user with username and password. Returns JWT token.
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Login successful - returns JWT token</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto.Username, dto.Password, ct);
        return Ok(new ApiResponse<LoginResponseDto>(true, "Login successful.", result));
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    /// <param name="dto">Current and new password</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Password mismatch or validation error</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest(new ApiResponse(false, "Passwords do not match."));

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var warning = await _authService.ChangePasswordAsync(userId, dto.NewPassword, ct);
        var msg = warning ?? "Password updated successfully.";
        return Ok(new ApiResponse(true, msg));
    }

    /// <summary>
    /// Reset password for a user (Admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="dto">New password</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Password reset successfully</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="404">User not found</response>
    [HttpPut("reset-password/{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(int userId, [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        var warning = await _authService.ResetPasswordAsync(userId, dto.NewPassword, ct);
        var msg = warning ?? "Password reset. User will be prompted to change it on next login.";
        return Ok(new ApiResponse(true, msg));
    }
}
