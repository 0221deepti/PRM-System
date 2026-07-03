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
    private readonly IEmailService _emailService;

    public AuthService(IUserRepository users, IEmployeeRepository employees, ITokenService tokenService, IEmailService emailService)
    {
        _users = users;
        _employees = employees;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<LoginResponseDto> LoginAsync(string username, string password, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new PrmAuthenticationException("Invalid credentials.");

        if (!user.IsActive)
            throw new PrmAuthenticationException("Account is deactivated.");

        var employee = await _employees.GetByUserIdAsync(user.Id, ct);
        var employeeId = employee?.Id ?? 0;

        var token = _tokenService.GenerateToken(user, employeeId);

        return new LoginResponseDto(token, user.ForcePasswordChange, user.Role?.Name ?? "User", user.FullName, user.Id, employeeId);
    }

    public async Task<string?> ChangePasswordAsync(int userId, string newPassword, CancellationToken ct)
    {
        ValidatePasswordStrength(newPassword);

        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ForcePasswordChange = false;

        _users.Update(user);
        await _users.SaveChangesAsync(ct);

        string? warningMessage = null;
        try
        {
            var placeholders = new Dictionary<string, string>
            {
                ["EmployeeName"] = user.FullName,
                ["Username"] = user.Username
            };
            var emailResult = await _emailService.SendTemplateEmailAsync("Password Changed Confirmation", user.Email, placeholders, ct);
            if (!emailResult.IsSuccess)
            {
                warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
            }
        }
        catch (Exception)
        {
            warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
        }
        return warningMessage;
    }

    public async Task<string?> ResetPasswordAsync(int targetUserId, string newPassword, CancellationToken ct)
    {
        ValidatePasswordStrength(newPassword);

        var user = await _users.GetByIdAsync(targetUserId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ForcePasswordChange = true;

        _users.Update(user);
        await _users.SaveChangesAsync(ct);

        string? warningMessage = null;
        try
        {
            var placeholders = new Dictionary<string, string>
            {
                ["EmployeeName"] = user.FullName,
                ["Username"] = user.Username
            };
            var emailResult = await _emailService.SendTemplateEmailAsync("Password Changed Confirmation", user.Email, placeholders, ct);
            if (!emailResult.IsSuccess)
            {
                warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
            }
        }
        catch (Exception)
        {
            warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
        }
        return warningMessage;
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
