using System.ComponentModel.DataAnnotations;

namespace PRM.Application.DTOs.User;

public record CreateUserDto(
    [Required(ErrorMessage = "Full Name is required.")] 
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Full Name must be between 2 and 200 characters.")] 
    string FullName,

    [Required(ErrorMessage = "Email is required.")] 
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")] 
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")] 
    string Email,

    [Required(ErrorMessage = "Username is required.")] 
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")] 
    string Username,

    [Required(ErrorMessage = "Password is required.")] 
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")] 
    string TemporaryPassword,

    [Required(ErrorMessage = "Role ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Role ID must be a valid positive integer.")] 
    int RoleId,

    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")] 
    string? Department = null);

public record UserSummaryDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    string RoleName,
    bool IsActive);
