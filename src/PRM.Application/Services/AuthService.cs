using PRM.Application.DTOs.Auth;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Exceptions;

namespace PRM.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IEmployeeRepository _employees;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository users, IEmployeeRepository employees, ITokenService tokenService)
    {
        _users = users;
        _employees = employees;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(string username, string password, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(username, ct)
                   ?? throw new EntityNotFoundException("Invalid credentials.");

        if (!user.IsActive)
            throw new DomainException("Account is deactivated.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new DomainException("Invalid credentials.");

        var employee = await _employees.GetByUserIdAsync(user.Id, ct);
        var employeeId = employee?.Id ?? 0;

        var token = _tokenService.GenerateToken(user, employeeId);

        return new LoginResponseDto(token, user.ForcePasswordChange, user.Role?.Name ?? "User", user.FullName, user.Id, employeeId);
    }

    public async Task ChangePasswordAsync(int userId, string newPassword, CancellationToken ct)
    {
        ValidatePasswordStrength(newPassword);

        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ForcePasswordChange = false;

        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(int targetUserId, string newPassword, CancellationToken ct)
    {
        ValidatePasswordStrength(newPassword);

        var user = await _users.GetByIdAsync(targetUserId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ForcePasswordChange = true;

        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    private static void ValidatePasswordStrength(string password)
    {
        if (password.Length < 8
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsDigit))
        {
            throw new DomainException(
                "Password must be 8+ characters with at least one uppercase letter and one digit.");
        }
    }
}
