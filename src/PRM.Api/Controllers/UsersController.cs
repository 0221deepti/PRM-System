using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.User;
using PRM.Application.Interfaces.Services;

namespace PRM.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var result = await _userService.CreateUserAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _userService.GetAllUsersAsync(ct);
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _userService.DeactivateUserAsync(id, ct);
        return Ok(new { message = "User deactivated." });
    }

    [HttpPut("{id:int}/reactivate")]
    public async Task<IActionResult> Reactivate(int id, CancellationToken ct)
    {
        await _userService.ReactivateUserAsync(id, ct);
        return Ok(new { message = "Account reactivated." });
    }
}
