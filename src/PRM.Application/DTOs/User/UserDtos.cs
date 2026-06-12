namespace PRM.Application.DTOs.User;

public record CreateUserDto(
    string FullName,
    string Email,
    string Username,
    string TemporaryPassword,
    int RoleId,
    string? Department = null);

public record UserSummaryDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    string RoleName,
    bool IsActive);
