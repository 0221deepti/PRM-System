using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.User;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

/// <summary>
/// User management - Admin only. Manage users with roles and permissions.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>
    /// Create a new user (Admin only)
    /// </summary>
    /// <param name="dto">User creation data with role assignment</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid input or user already exists</response>
    /// <response code="403">Forbidden - Admin role required</response>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var result = await _userService.CreateUserAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">List of all users</response>
    /// <response code="403">Forbidden - Admin role required</response>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _userService.GetAllUsersAsync(ct);
        return Ok(users);
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">User details</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// Deactivate a user account (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">User deactivated</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _userService.DeactivateUserAsync(id, ct);
        return Ok(new { message = "User deactivated." });
    }

    /// <summary>
    /// Reactivate a user account (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">User reactivated</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id:int}/reactivate")]
    public async Task<IActionResult> Reactivate(int id, CancellationToken ct)
    {
        await _userService.ReactivateUserAsync(id, ct);
        return Ok(new { message = "Account reactivated." });
    }
}
