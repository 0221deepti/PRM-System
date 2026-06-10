using PRM.Application.DTOs.User;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IEmployeeRepository _employees;
    private readonly IAllocationRepository _allocations;

    public UserService(IUserRepository users, IEmployeeRepository employees, IAllocationRepository allocations)
    {
        _users = users;
        _employees = employees;
        _allocations = allocations;
    }

    public async Task<UserSummaryDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct)
    {
        ValidatePasswordStrength(dto.TemporaryPassword);

        if (await _users.ExistsAsync(dto.Username, dto.Email, ct))
            throw new DomainException("A user with this username or email already exists.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.TemporaryPassword),
            Role = dto.Role,
            IsActive = true,
            ForcePasswordChange = true
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        // Create Employee record for Manager and Employee roles
        if (dto.Role == Domain.Enums.UserRole.Manager || dto.Role == Domain.Enums.UserRole.Employee)
        {
            var employee = new Employee
            {
                UserId = user.Id,
                Department = "Unassigned",
                Status = Domain.Enums.EmployeeStatus.Bench
            };
            await _employees.AddAsync(employee, ct);
            await _employees.SaveChangesAsync(ct);
        }

        return MapToDto(user);
    }

    public async Task<IEnumerable<UserSummaryDto>> GetAllUsersAsync(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        return users.Select(MapToDto);
    }

    public async Task DeactivateUserAsync(int userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        user.IsActive = false;
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task ReactivateUserAsync(int userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new EntityNotFoundException("User not found.");

        user.IsActive = true;
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task<UserSummaryDto?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserSummaryDto?> GetByIdAsync(int userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        return user == null ? null : MapToDto(user);
    }

    private static UserSummaryDto MapToDto(User user) =>
        new(user.Id, user.Username, user.FullName, user.Email, user.Role, user.IsActive);

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
