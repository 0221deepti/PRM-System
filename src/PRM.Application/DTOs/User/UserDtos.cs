using PRM.Domain.Enums;

namespace PRM.Application.DTOs.User;

public record CreateUserDto(
    string FullName,
    string Email,
    string Username,
    string TemporaryPassword,
    UserRole Role);

public record UserSummaryDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive);
