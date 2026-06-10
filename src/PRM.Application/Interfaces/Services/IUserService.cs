using PRM.Application.DTOs.User;
using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserSummaryDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct);
    Task<IEnumerable<UserSummaryDto>> GetAllUsersAsync(CancellationToken ct);
    Task DeactivateUserAsync(int userId, CancellationToken ct);
    Task ReactivateUserAsync(int userId, CancellationToken ct);
    Task<UserSummaryDto?> GetByUsernameAsync(string username, CancellationToken ct);
    Task<UserSummaryDto?> GetByIdAsync(int userId, CancellationToken ct);
}
