using PRM.Application.DTOs.Auth;

namespace PRM.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(string username, string password, CancellationToken ct);
    Task ChangePasswordAsync(int userId, string newPassword, CancellationToken ct);
    Task ResetPasswordAsync(int targetUserId, string newPassword, CancellationToken ct);
}
