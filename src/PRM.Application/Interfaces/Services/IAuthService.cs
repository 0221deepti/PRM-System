using PRM.Application.DTOs.Auth;

namespace PRM.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(string username, string password, CancellationToken ct);
    Task<string?> ChangePasswordAsync(int userId, string newPassword, CancellationToken ct);
    Task<string?> ResetPasswordAsync(int targetUserId, string newPassword, CancellationToken ct);
}
